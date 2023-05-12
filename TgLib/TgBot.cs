using System.Collections.ObjectModel;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgLib.Commands;
using TgLib.Commands.Exceptions;

namespace TgLib
{
    /// <summary>
    /// Базовый клиент Telegram-бота
    /// </summary>
    public class TgBot : TelegramBotClient
    {
        #region Public fields
        /// <summary>
        /// Словарь всех зарегистрированных команд. Ключ словаря - название команды
        /// </summary>
        public ReadOnlyDictionary<string, TgCommand> RegisteredCommands { get { return new(_registeredCommands); } }
        #endregion

        #region Internal fields
        internal readonly Dictionary<string, TgCommand> _registeredCommands = new();
        internal UserCache cache = null!;
        internal Interactivity interact = null!;
        #endregion

        #region Public methods
        /// <summary>
        /// Создаёт новый экземпляр <see cref="TgBot"/> с указанным токеном
        /// </summary>
        /// <param name="token">Токен Telegram-бота</param>
        /// <exception cref="ArgumentException"></exception>
        public TgBot(string token) : base(token) { }

        /// <summary>
        /// Регистрирует класс <typeparamref name="T"/> как список команд для бота.
        /// </summary>
        /// <typeparam name="T">Класс, содержащий методы для выполнения как команды</typeparam>
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

        /// <summary>
        /// Пытается подключиться к серверам Telegram и начать прослушивание приходящих сообщений
        /// </summary>
        public async Task ConnectAsync()
        {
            cache = new UserCache(this);
            interact = new Interactivity();
            this.StartReceiving(InternalUpdateHandler, InternalErrorHandler);
            await Task.CompletedTask;
        }
        #endregion

        #region Internal methods
        internal async Task InternalUpdateHandler(ITelegramBotClient client, Update update, CancellationToken clsToken)
        {
            if (update.Message is not { } message)
                return;
            if (message.Text is not { } messageText)
                return;
            long chatId = message.Chat.Id;

            TgUser user = cache.GetOrCreateSession(chatId);
            if (messageText.StartsWith('/') && messageText.Length > 2 && !"0123456789\"\'".Contains(messageText[1]))
            {
                List<string> commandArgs = messageText.Split('\'', '\"')
                        .Select((element, index) => index % 2 == 0 ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : new[] { element })
                        .SelectMany(element => element)
                        .Where(element => !string.IsNullOrEmpty(element))
                        .ToList();
                string commandName = commandArgs[0][1..].ToLower();
                commandArgs.RemoveAt(0);

                foreach (KeyValuePair<string, TgCommand> pair in _registeredCommands.Where((x) => x.Key.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
                {
                    MethodInfo method = pair.Value.Method;
                    ParameterInfo[] methodArgs = method.GetParameters();
                    if (methodArgs.Length == 1)
                    {
                        _ = Task.Run(() => pair.Value.Invoke(this, user), clsToken);
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
                            _ = Task.Run(() => pair.Value.Invoke(this, user, args), clsToken);
                        }
                        catch (CommandErroredException err)
                        {
                            _ = CommandErrored?.Invoke(this, err);
                        }
                        return;
                    }
                }
                _ = CommandErrored?.Invoke(this, new CommandNotFoundException(commandName));
            }
            else
            {
                if (interact.TryGetRequest(user, out Request? req))
                {
                    interact.SetCompleted(user, messageText);
                }
                else
                {
                    _ = MessageRecieved?.Invoke(this, message);
                }
            }
            await Task.CompletedTask;
        }

        internal Task InternalErrorHandler(ITelegramBotClient _1, Exception exception, CancellationToken _2)
        {
            _ = PollingErrored?.Invoke(this, exception)!;
            return Task.CompletedTask;
        }
        #endregion

        #region Delegates & Events
        /// <summary>
        /// Делегат, описывающий обработчик ошибок цикла событий
        /// </summary>
        /// <param name="client">Клиент бота, в котором возникло исключение</param>
        /// <param name="ex">Вызыванное исключение</param>
        public delegate Task PollingErrorHandler(TgBot client, Exception ex);

        /// <summary>
        /// Делегат, описывающий обработчик входящих сообщений, не являющихся командами
        /// </summary>
        /// <param name="client">Клиент бота, которому пришло сообщение</param>
        /// <param name="msg">Объект полученного сообщения</param>
        public delegate Task MessageRecievedHandler(TgBot client, Message msg);

        /// <summary>
        /// Делегат, описывающий обработчик ошибок выполнения команды
        /// </summary>
        /// <param name="client">Клиент бота, в котором возникло исключение</param>
        /// <param name="ex">Вызыванное исключение</param>
        public delegate Task CommandErroredHandler(TgBot client, Exception ex);

        /// <summary>
        /// Вызывается, когда возникает ошибка в цикле событий
        /// </summary>
        public event PollingErrorHandler? PollingErrored;

        /// <summary>
        /// Вызывается, когда приходит сообщение, не являющееся командой
        /// </summary>
        public event MessageRecievedHandler? MessageRecieved;

        /// <summary>
        /// Вызывается, когда возникает ошибка во время выполнения команды
        /// </summary>
        public event CommandErroredHandler? CommandErrored;
        #endregion
    }
}