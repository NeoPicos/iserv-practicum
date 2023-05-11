using System.Reflection;
using TgLib.Commands.Exceptions;

namespace TgLib
{
    namespace Commands
    {
        public class TgCommand
        {
            internal MethodInfo Method { get; set; }
            public string Name { get; internal set; }
            public string[] Aliases { get; internal set; }

            internal TgCommand(MethodInfo method, string? name = null, string[]? aliases = null)
            {
                Method = method;
                Name = (string.IsNullOrWhiteSpace(name) ? method.Name : name).ToLower();
                Aliases = aliases ?? Array.Empty<string>();
            }

            public async Task Invoke(TgBot bot, TgUser user)
            {
                await Invoke(bot, user, new List<object?>());
            }

            public async Task Invoke(TgBot bot, TgUser user, IEnumerable<object?> args)
            {
                CommandContext ctx = new(bot, user, this);
                List<object?> arguments = new() { ctx };
                arguments.AddRange(args);
                try
                {
                    await (Task)Method.Invoke(null, arguments.ToArray())!;
                }
                catch (TargetInvocationException ex)
                {
                    throw new CommandErroredException(ex.InnerException!.Message, ex.InnerException, ctx);
                }
            }
        }

        public class CommandContext
        {
            public TgBot Client { get; internal set; }
            public TgUser User { get; internal set; }
            public TgCommand Command { get; internal set; }

            internal CommandContext(TgBot client, TgUser chat, TgCommand command)
            {
                Client = client;
                User = chat;
                Command = command;
            }

            public async Task ResponseAsync(string message)
            {
                await User.SendMessage(message);
            }
        }

        #region Attributes
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        public class CommandAttribute : Attribute
        {
            public string? Name;

            public CommandAttribute() { Name = null; }

            public CommandAttribute(string name) { Name = name; }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
        public class AliasAttribute : Attribute
        {
            public string[] Aliases { get; set; }

            public AliasAttribute()
            {
                throw new ArgumentException("Alias must be provided");
            }

            public AliasAttribute(params string[] names)
            {
                if (names.Distinct().Count() != names.Length)
                    throw new ArgumentException("Aliases list contains matching names");
                Aliases = names;
            }
        }

        [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
        public class RemainingTextAttribute : Attribute { }
        #endregion
    }
}
