using System.Runtime.Serialization;

namespace TgLib.Commands.Exceptions
{
    /// <summary>
    /// Исключение, вызываемое, когда была предпринята использовать несуществующую команду
    /// </summary>
    [Serializable]
    public class CommandNotFoundException : Exception, ISerializable
    {
        /// <summary>
        /// Название команды, которая была запрошена
        /// </summary>
        public string TriedCommandName = string.Empty;

        /// <summary>
        /// Создаёт новый экземпляр <see cref="CommandNotFoundException"/> с указанным именем команды
        /// </summary>
        /// <param name="command">Название запрошенной команды</param>
        public CommandNotFoundException(string? command) : base("Specified command was not found.")
        {
            TriedCommandName = command ?? string.Empty;
        }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="CommandNotFoundException"/> с указанным именем команды
        /// </summary>
        /// <param name="command">Название запрошенной команды</param>
        /// <param name="innerException">Внутреннее исключение</param>
        public CommandNotFoundException(string? command, Exception? innerException) : base("Specified command was not found.", innerException)
        {
            TriedCommandName = command ?? string.Empty;
        }

        /// <inheritdoc/>
        protected CommandNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            TriedCommandName = info.GetString(nameof(TriedCommandName)) ?? string.Empty;
        }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(TriedCommandName), TriedCommandName, TriedCommandName.GetType());
            base.GetObjectData(info, context);
        }
    }

    /// <summary>
    /// Исключение, вызываемое, когда во время выполнения команды произошла ошибка
    /// </summary>
    public class CommandErroredException : Exception, ISerializable
    {
        /// <summary>
        /// Контекст выполняемой команды
        /// </summary>
        public CommandContext CommandCtx;

        /// <summary>
        /// Создаёт новый экземпляр <see cref="CommandErroredException"/> с указанным сообщением об о шибке и контекстом команды 
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="commandCtx">Контекст команды</param>
        public CommandErroredException(string? message, CommandContext commandCtx) : base(message)
        {
            CommandCtx = commandCtx;
        }

        /// <summary>
        /// Создаёт новый экземпляр <see cref="CommandErroredException"/> с указанным сообщением об о шибке и контекстом команды 
        /// </summary>
        /// <param name="message">Сообщение об ошибке</param>
        /// <param name="innerException">Внутреннее исключение</param>
        /// <param name="commandCtx">Контекст команды</param>
        public CommandErroredException(string? message, Exception? innerException, CommandContext commandCtx) : base(message, innerException)
        {
            CommandCtx = commandCtx;
        }

        /// <inheritdoc/>
        protected CommandErroredException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            CommandCtx = (CommandContext)info.GetValue(nameof(CommandCtx), CommandCtx!.GetType())!;
        }

        /// <inheritdoc/>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(CommandCtx), CommandCtx, CommandCtx.GetType());
            base.GetObjectData(info, context);
        }
    }
}
