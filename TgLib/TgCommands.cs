using System.Reflection;
using Telegram.Bot.Types;
using TgLib.Commands.Exceptions;

namespace TgLib
{
    namespace Commands
    {
        /// <summary>
        /// Класс, описывающий команду
        /// </summary>
        public class TgCommand
        {
            #region Public Fields
            /// <summary>
            /// Название команды, по нему происходит вызов
            /// </summary>
            public string Name { get; internal set; }
            /// <summary>
            /// Псевдонимы команды
            /// </summary>
            public string[] Aliases { get; internal set; }
            #endregion

            #region Internal fields
            internal MethodInfo Method { get; set; }
            #endregion

            #region Public methods
            /// <summary>
            /// Вызывать эту команду без аргументов для определённого пользователя
            /// </summary>
            /// <param name="bot">Экземпляр бота, в котором нужно провести выполнение команды</param>
            /// <param name="user">Пользователь, для которого нужно провести выполнение команды</param>
            public async Task Invoke(TgBot bot, TgUser user)
            {
                await Invoke(bot, user, new List<object?>());
            }

            /// <summary>
            /// Вызывать эту команду c аргументами для определённого пользователя
            /// </summary>
            /// <param name="bot">Экземпляр бота, в котором нужно провести выполнение команды</param>
            /// <param name="user">Пользователь, для которого нужно провести выполнение команды</param>
            /// <param name="args">Список аргументов команды, чья длина и типы элементов совпадают с аргументами команды</param>
            public async Task Invoke(TgBot bot, TgUser user, IEnumerable<object?> args)
            {
                CommandContext ctx = new(bot, user, this);
                List<object?> arguments = new() { ctx };
                arguments.AddRange(args);
                try
                {
                    await (Task)Method.Invoke(null, arguments.ToArray())!;
                }
                catch (Exception ex)
                {
                    ctx.Client.RaiseCommandErrored(ctx, ex);
                }
            }
            #endregion

            #region Internal methods
            internal TgCommand(MethodInfo method, string? name = null, string[]? aliases = null)
            {
                Method = method;
                Name = (string.IsNullOrWhiteSpace(name) ? method.Name : name).ToLower();
                Aliases = aliases ?? Array.Empty<string>();
            }
            #endregion
        }

        /// <summary>
        /// Класс, представляющий контекст выполнения команды
        /// </summary>
        public class CommandContext
        {
            #region Public fields
            /// <summary>
            /// Клиент бота, в котором выполняется команда
            /// </summary>
            public TgBot Client { get; internal set; }
            /// <summary>
            /// Пользователь, который вызвал команду или для которого она выполняется
            /// </summary>
            public TgUser User { get; internal set; }
            /// <summary>
            /// Выполняемая команда
            /// </summary>
            public TgCommand Command { get; internal set; }
            #endregion

            #region Public methods
            /// <summary>
            /// Создаёт новый контекст команды <para>
            /// Использование этого конструктора напрямую не рекомендуется</para>
            /// </summary>
            /// <param name="client"></param>
            /// <param name="chat"></param>
            /// <param name="command"></param>
            public CommandContext(TgBot client, TgUser chat, TgCommand command)
            {
                Client = client;
                User = chat;
                Command = command;
            }

            /// <summary>
            /// Отправить текстовый ответ в рамках контекста пользователя
            /// </summary>
            /// <param name="message">Отправляемое сообщение</param>
            /// <returns>Экземпляр отправленного сообщения</returns>
            public async Task<Message> RespondAsync(string message)
            {
                return await User.SendMessage(message);
            }

            /// <summary>
            /// Останавливает выполнение метода до получения ввода пользователя
            /// </summary>
            /// <returns></returns>
            public async Task<string> WaitForUserInput()
            {
                Task<string> TcsTask = Client.interact.AddRequest(User).Tcs.Task;
                await TcsTask;
                return TcsTask.Result;
            }
            #endregion
        }

        #region Attributes
        /// <summary>
        /// Атрибут метода, помечающий что он является выполняемой командой
        /// </summary>
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        public class CommandAttribute : Attribute
        {
            internal string? Name;

            /// <summary>
            /// Помечает метод как выполняемую команду, и устанавливает название метода как название команды
            /// </summary>
            public CommandAttribute() { Name = null; }

            /// <summary>
            /// Помечает метод как выполняемую команду с указанным названием
            /// </summary>
            /// <param name="name">Название команды</param>
            public CommandAttribute(string name) { Name = name; }
        }

        /// <summary>
        /// Атрибут команды-метода, добавляющий псевдоним для команды
        /// </summary>
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        public class AliasAttribute : Attribute
        {
            internal string[] Aliases { get; set; }

            /// <summary>
            /// Добавляет псевдонимы к созданной команде
            /// </summary>
            /// <param name="names">Псевдонимы для команды</param>
            public AliasAttribute(params string[] names)
            {
                if (names.Distinct().Count() != names.Length)
                    throw new ArgumentException("Aliases list contains matching names");
                Aliases = names;
            }
        }

        /// <summary>
        /// Атрибут, показывающий что весь остальной текстовый ввод должен быть сохранён в параметре без обработки
        /// </summary>
        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class RemainingTextAttribute : Attribute { }
        #endregion
    }
}
