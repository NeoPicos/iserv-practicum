using Telegram.Bot.Types;
using Telegram.Bot;
using TgLib;
using TgLib.Commands;

namespace Practicum
{
    public static class Program
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
            DbConnection.Connect(settings[1].Trim());

            bot.RegisterCommands<Commands>();
            bot.CommandErrored += CommandErrored;
            await bot.ConnectAsync();

            User me = await bot.GetMeAsync();
            Console.WriteLine($"Бот запущен и прослушивает как @{me.Username}");

            await Task.Delay(-1);
        }

        private static async Task CommandErrored(TgBot client, Exception ex)
        {
            Console.WriteLine($"{ex}");
            await Task.CompletedTask;
        }
    }
}

