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
        public ReadOnlyCollection<KeyValuePair<string, TgCommand>> RegisteredCommands { get { return new(_registeredCommands); } }
        #endregion

        #region Internal fields
        internal readonly List<KeyValuePair<string, TgCommand>> _registeredCommands = new();
        internal readonly InteractivityModule _interactivity;
        internal readonly CacheModule _cache = null!;
        #endregion

        #region Public methods
        /// <summary>
        /// Создаёт новый экземпляр <see cref="TgBot"/> с указанным токеном
        /// </summary>
        /// <param name="token">Токен Telegram-бота</param>
        /// <exception cref="ArgumentException"></exception>
        public TgBot(string token) : base(token) 
        {
            _interactivity = new InteractivityModule();
            _cache = new CacheModule(this);
        }

        /// <summary>
        /// Регистрирует класс <typeparamref name="T"/> как список команд для бота.
        /// </summary>
        /// <typeparam name="T">Класс, содержащий методы для выполнения как команды</typeparam>
        public void RegisterCommands<T>() where T : class
        {
            // Просматриваем все методы с атрибутом CommandAttribute
            foreach (MethodInfo method in typeof(T).GetMethods().Where(x => !x.Name.StartsWith("get_") && !x.Name.StartsWith("set_")))
            {
                CommandAttribute? commandAttr = method.GetCustomAttribute<CommandAttribute>();
                if (commandAttr is null)
                    continue;

                // Первый параметр метода всегда должен быть CommandContext
                ParameterInfo[] methodArgs = method.GetParameters();
                if (methodArgs.Length == 0 || methodArgs[0].ParameterType != typeof(CommandContext))
                    continue;

                // Регистрируем метод как команду и записываем её псевдонимы
                // Псевдонимы команд - те же методы, но под другими именами
                TgCommand command = new(method, commandAttr.Name);
                AliasAttribute? aliasAttribute = method.GetCustomAttribute<AliasAttribute>();
                if (aliasAttribute is not null)
                {
                    command.Aliases = aliasAttribute.Aliases;
                    foreach (string i in command.Aliases)
                    {
                        _registeredCommands.Add(new(i.ToLower(), command));
                    }
                }

                _registeredCommands.Add(new(command.Name.ToLower(), command));
            }
        }

        /// <summary>
        /// Подключается к Telegram и начинает цикл обработки сообщений
        /// </summary>
        public async Task ConnectAsync()
        {
            _cache.AutoclearSetRunning(true);
            this.StartReceiving(InternalUpdateHandler, InternalErrorHandler, new() { ThrowPendingUpdates = true });
            await Task.CompletedTask;
        }

        /// <summary>
        /// Вызывает команду с указанным названием и аргументами
        /// </summary>
        /// <param name="commandName"></param>
        /// <param name="commandArgs"></param>
        /// <param name="user"></param>
        public void InvokeCommand(string commandName, TgUser user, List<string> commandArgs)
        {
            if (user.IsPendingInput)
                user.CancelPendingInput();
            foreach (KeyValuePair<string, TgCommand> pair in _registeredCommands.Where((x) => x.Key.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)))
            {
                if (InvokeCommand(pair.Value, user, commandArgs))
                    return;
            }
            _ = CommandErrored?.Invoke(new CommandContext(this, user, null!), new CommandNotFoundException(commandName));
        }
        #endregion

        #region Internal methods
        internal async Task InternalUpdateHandler(ITelegramBotClient client, Update update, CancellationToken clsToken)
        {
            if (update.CallbackQuery is { } callback)
            {
                CallbackQueryRecieved?.Invoke(this, _cache.GetOrCreateUser(callback.From.Id), callback);
                return;
            }

            if (update.Message is not { } message)
                return;
            if (message.Text is not { } messageText)
                return;
            long chatId = message.Chat.Id;
            TgUser user = _cache.GetOrCreateUser(chatId);
            user.LastMessage = message;

            if (messageText.StartsWith('/') && messageText.Length > 2 && !"0123456789\"\'".Contains(messageText[1]))
            {
                List<string> commandArgs = messageText.Split('\'', '\"')
                        .Select((element, index) => index % 2 == 0 ? element.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) : new[] { element })
                        .SelectMany(element => element)
                        .Where(element => !string.IsNullOrEmpty(element))
                        .ToList();
                string commandName = commandArgs[0][1..].ToLower();
                commandArgs.RemoveAt(0);
                InvokeCommand(commandName, user, commandArgs);
            }
            else
            {
                if (_interactivity.TryGetRequest(user, out Request? req))
                {
                    _interactivity.SetCompleted(user, messageText);
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

        internal void RaiseCommandErrored(CommandContext ctx, Exception ex)
        {
            _ = CommandErrored?.Invoke(ctx, ex);
        }

        internal bool InvokeCommand(TgCommand command, TgUser user, List<string> commandArgs)
        {
            MethodInfo method = command.Method;
            ParameterInfo[] methodArgs = method.GetParameters();
            if (methodArgs.Length == 1)
            {
                _ = Task.Run(() => command.Invoke(this, user));
                return true;
            }
            else
            {
                if (commandArgs.Count < methodArgs.Length - 1)
                    return false;
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
                    return false;
                }

                _ = Task.Run(() => command.Invoke(this, user, args));
                return true;
            }
        }
        #endregion

        #region Delegates & Events
        /// <summary>
        /// Делегат, описывающий обработчик ошибок цикла событий
        /// </summary>
        /// <param name="sender">Клиент бота, в котором возникло исключение</param>
        /// <param name="ex">Вызванное исключение</param>
        public delegate Task PollingErrorHandler(TgBot sender, Exception ex);

        /// <summary>
        /// Делегат, описывающий обработчик входящих сообщений, не являющихся командами
        /// </summary>
        /// <param name="sender">Клиент бота, которому пришло сообщение</param>
        /// <param name="msg">Объект полученного сообщения</param>
        public delegate Task MessageRecievedHandler(TgBot sender, Message msg);

        /// <summary>
        /// Делегат, описывающий обработчик ошибок выполнения команды
        /// </summary>
        /// <param name="client">Клиент бота, в котором возникло исключение</param>
        /// <param name="ex">Вызванное исключение</param>
        public delegate Task CommandErroredHandler(CommandContext client, Exception ex);

        /// <summary>
        /// Делегат, описывающий обработчик полученного callback запроса
        /// </summary>
        /// <param name="sender">Клиент бота, которому пришло событие</param>
        /// <param name="user"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public delegate Task CallbackQueryRecievedHandler(TgBot sender, TgUser user, CallbackQuery query);

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

        /// <summary>
        /// Вызывается, когда получен новый callback запрос
        /// </summary>
        public event CallbackQueryRecievedHandler? CallbackQueryRecieved;
        #endregion
    }
}