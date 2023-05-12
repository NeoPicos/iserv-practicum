using Telegram.Bot.Types;
using Telegram.Bot;
using TgLib;
using TgLib.Commands;

namespace Practicum
{
    internal static class Program
    {
        public static void Main()
        {
            try
            {
                MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
                Environment.Exit(1);
            }
        }

        public static async Task MainAsync()
        {
            string[] settings = System.IO.File.ReadAllLines("settings.cfg");
            TgBot bot = new(settings[0].Trim());

            bot.RegisterCommands<Commands>();
            await bot.ConnectAsync();

            User me = await bot.GetMeAsync();
            Console.WriteLine($"Бот запущен и прослушивает как @{me.Username}");

            await Task.Delay(-1);
        }

        public class Commands
        {
            [Command]
            public static async Task Hello(CommandContext ctx)
            {
                await ctx.ResponseAsync($"Hello! This chat id is {ctx.User.ChatID}.");
            }
        }
    }
}

