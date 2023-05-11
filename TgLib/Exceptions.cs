using System.Runtime.Serialization;

namespace TgLib.Commands.Exceptions
{
    [Serializable]
    public class CommandNotFoundException : Exception, ISerializable
    {
        public string TriedCommandName = string.Empty;

        public CommandNotFoundException(string? command) : base("Specified command was not found.")
        {
            TriedCommandName = command ?? string.Empty;
        }

        public CommandNotFoundException(string? command, Exception? innerException) : base("Specified command was not found.", innerException)
        {
            TriedCommandName = command ?? string.Empty;
        }

        protected CommandNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            TriedCommandName = info.GetString(nameof(TriedCommandName)) ?? string.Empty;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(TriedCommandName), TriedCommandName, TriedCommandName.GetType());
            base.GetObjectData(info, context);
        }
    }

    public class CommandErroredException : Exception, ISerializable
    {
        public CommandContext CommandCtx;

        public CommandErroredException(string? message, CommandContext commandCtx) : base(message)
        {
            CommandCtx = commandCtx;
        }

        public CommandErroredException(string? message, Exception? innerException, CommandContext commandCtx) : base(message, innerException)
        {
            CommandCtx = commandCtx;
        }

        protected CommandErroredException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            CommandCtx = (CommandContext)info.GetValue(nameof(CommandCtx), CommandCtx!.GetType())!;
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(CommandCtx), CommandCtx, CommandCtx.GetType());
            base.GetObjectData(info, context);
        }
    }
}
