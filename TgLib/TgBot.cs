using System.Collections.ObjectModel;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgLib.Commands;
using TgLib.Commands.Exceptions;

namespace TgLib
{
    public class TgBot : TelegramBotClient
    {
        public ReadOnlyDictionary<string, TgCommand> RegisteredCommands { get { return new(_registeredCommands); } }

        internal readonly Dictionary<string, TgCommand> _registeredCommands = new();

        public TgBot(string token) : base(token){ }

        public void RegisterCommands<T>() where T : class
        {
            foreach (MethodInfo method in typeof(T).GetMethods().Where(x => !x.Name.StartsWith("get_") && !x.Name.StartsWith("set_")))
            {
                CommandAttribute? commandAttr = method.GetCustomAttribute<CommandAttribute>();
                if (commandAttr is null)
                    continue;

                ParameterInfo[] methodArgs = method.GetParameters();
                if (methodArgs.Length == 0 || methodArgs[0].ParameterType != typeof(CommandContext))
                    continue;
                TgCommand command = new(method, commandAttr.Name);

                AliasAttribute? aliasAttribute = method.GetCustomAttribute<AliasAttribute>();
                if (aliasAttribute is not null)
                {
                    command.Aliases = aliasAttribute.Aliases;
                    foreach (string i in command.Aliases)
                    {
                        _registeredCommands.Add(i, command);
                    }
                }

                _registeredCommands.Add(command.Name, command);
            }
        }

        public async Task ConnectAsync()
        {
            TgCache.Initialize(this);
            this.StartReceiving(InternalUpdateHandler, InternalErrorHandler);
            await Task.CompletedTask;
        }

        internal async Task InternalUpdateHandler(ITelegramBotClient client, Update update, CancellationToken clsToken)
        {
            if (update.Message is not { } message)
                return;
            if (message.Text is not { } messageText)
                return;
            long chatId = message.Chat.Id;

            TgUser user = TgCache.GetOrCreateSession(chatId);
            if (messageText.StartsWith('/') && messageText.Length > 2 && !"0123456789\"\'".Contains(messageText[1]))
            {
                List<string> commandArgs = messageText.Split('\'', '\"')
                        .Select((element, index) => index % 2 == 0 ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : new[] { element })
                        .SelectMany(element => element)
                        .Where(element => !string.IsNullOrEmpty(element))
                        .ToList();
                string commandName = commandArgs[0][1..].ToLower();
                commandArgs.RemoveAt(0);

                foreach (KeyValuePair<string, TgCommand> pair in _registeredCommands.Where((x) => x.Key == commandName))
                {
                    MethodInfo method = pair.Value.Method;
                    ParameterInfo[] methodArgs = method.GetParameters();
                    if (methodArgs.Length == 1)
                    {
                        await pair.Value.Invoke(this, user);
                        return;
                    }
                    else
                    {
                        if (commandArgs.Count < methodArgs.Length - 1)
                            continue;
                        List<object> args = new();
                        try
                        {
                            for (int i = 0; i < methodArgs.Length - 1; i++)
                            {
                                ParameterInfo methodArg = methodArgs[i + 1];
                                if (methodArg.GetCustomAttribute<RemainingTextAttribute>() is not null)
                                {
                                    string text = "";
                                    while (i < commandArgs.Count)
                                        text += commandArgs[i++] + " ";
                                    args.Add(text[..(text.Length - 1)]);
                                    break;
                                }
                                else
                                    args.Add(Convert.ChangeType(commandArgs[i], methodArg.ParameterType));
                            }
                        }
                        catch
                        {
                            continue;
                        }

                        try
                        {
                            _ = pair.Value.Invoke(this, user, args);
                        }
                        catch(CommandErroredException err)
                        {
                            if(CommandErrored is not null)
                                _ = CommandErrored(this, err);
                        }
                        return;
                    }
                }
                if(CommandErrored is not null)
                    _ = CommandErrored(this, new CommandNotFoundException(commandName));
            }
            else
            {
                if (!user.PendingInput)
                {
                    // TODO: Интерактивность
                }
                else
                {
                    if (MessageRecieved is not null)
                        _ = MessageRecieved(this, message);
                }
            }
        }

        internal async Task InternalErrorHandler(ITelegramBotClient _1, Exception exception, CancellationToken _2)
        {
            if(PollingErrored is not null)
                await PollingErrored.Invoke(this, exception);
        }

        public delegate Task PollingErrorHandler(TgBot client, Exception ex);

        public delegate Task MessageRecievedHandler(TgBot client, Message msg);

        public delegate Task CommandErroredHandler(TgBot client, Exception ex);

        public event PollingErrorHandler? PollingErrored;

        public event MessageRecievedHandler? MessageRecieved;

        public event CommandErroredHandler? CommandErrored;
    }
}