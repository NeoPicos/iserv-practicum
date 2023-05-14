using Telegram.Bot.Types;
using Telegram.Bot;
using TgLib;
using TgLib.Commands;
using TgLib.Commands.Exceptions;

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
            bot.CallbackQueryRecieved += Callbacks.CallbackQueryRecieved;

            await bot.ConnectAsync();
            User me = await bot.GetMeAsync();
            Console.WriteLine($"Бот запущен и прослушивает как @{me.Username}");

            await Task.Delay(-1);
        }

        private static async Task CommandErrored(CommandContext ctx, Exception ex)
        {
            if(ex is CommandNotFoundException)
            {
                await ctx.RespondAsync("Я не понял твоей команды :(");
            }
            else
            {
                await ctx.RespondAsync("Во время выполнения команды произошла ошибка...");
                Console.WriteLine($"Err: {ex.InnerException!.Message}");
            }
        }
    }
}

