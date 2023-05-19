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
                // Весь код должен выполняться в отдельном потоке
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
            string[] settings = File.ReadAllLines("settings.cfg");
            TgBot bot = new(settings[0].Trim());
            DbConnection.Connect(settings[1].Trim());

            bot.RegisterCommands<Commands>();
            bot.CommandErrored += CommandErrored;
            bot.CallbackQueryRecieved += Callbacks.CallbackQueryRecieved;

            await bot.ConnectAsync();
            Telegram.Bot.Types.User me = await bot.GetMeAsync();
            Console.WriteLine($"Бот запущен и прослушивает как @{me.Username}");

            await Task.Delay(-1);
        }

        private static async Task CommandErrored(CommandContext ctx, Exception ex)
        {
            if(ex is CommandNotFoundException)
            {
                await ctx.RespondAsync("Я не понял твоей команды :(");
            }
            else if (ex is CommandErroredException)
            {
                await ctx.RespondAsync("Во время выполнения команды произошла ошибка...");
                Console.WriteLine($"Err: {ex}");
            }
        }

        public static string Remaining(TimeSpan span)
        {
            Func<int, string, string, string, string> gen = D1gitalLibrary.DUtilities.NumDeclension;
            if (span <= TimeSpan.Zero)
                return "(уже прошло)";
            if (span.Days > 365)
                return $"{gen(span.Days / 365, "год", "года", "лет")} и {gen(span.Days % 365, "день", "дня", "дней")}";
            if (span.Days > 14)
                return gen(span.Days, "день", "дня", "дней");
            if (span.Days > 0)
                return $"{gen(span.Days, "день", "дня", "дней")} и {gen(span.Hours, "час", "часа", "часов")}";
            if (span.Hours > 0)
                return $"{gen(span.Hours, "час", "часа", "часов")} и {gen(span.Minutes, "минуту", "минуты", "минут")}";
            if (span.Minutes > 0)
                return $"{gen(span.Minutes, "минуту", "минуты", "минут")} и {gen(span.Seconds, "секунду", "секунды", "секунд")}";
            if (span.Seconds > 1)
                return gen(span.Seconds, "секунду", "секунды", "секунд");
            return "меньше секунды";
        }
    }
}

