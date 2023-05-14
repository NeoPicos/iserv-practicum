using System.Globalization;
using TgLib.Commands;

namespace Practicum
{
    internal class Commands
    {
        // В этом классе описываются все методы, доступные боту.
        // Команды регистрируются в порядке их инициализации в этом файле
        // Для правильной работы метода должны быть выполнены следующие условия:
        // - Возвращаемый тип метода - async Task
        // - Первый аргумент метода - CommandContext
        // - Остальные аргументы должны быть приводимы из string
        #region Public commands
        [Command]
        public static async Task Start(CommandContext ctx)
        {
            await ctx.RespondAsync("👋 Привет! Здесь скоро будет информация об использовании бота и его функциях.");
            await Task.Delay(1500);
            await ctx.RespondAsync("📝 Скажи, как мне к тебе обращаться?");
            string name = await ctx.WaitForUserInput();
            while (string.IsNullOrWhiteSpace(name) || name.Length < 2 || name.Length > 64)
            {
                await ctx.RespondAsync("⚠️ Похоже, что это обращение слишком длинное или слишком короткое.\nПожалуйста, придумай другое");
                name = await ctx.WaitForUserInput();
            }
            await ctx.RespondAsync($"Прекрасно, я запомню тебя как {name}");

            DbConnection.ExecuteNonQuery(
                $"INSERT INTO users (`id`, `name`) VALUES ({ctx.User.ChatID}, @NAME) ON DUPLICATE KEY UPDATE `name`=@NAME;",
                new() { { "@NAME", name } });
        }

        [Command]
        [Alias("Menu")]
        public static async Task MainMenu(CommandContext ctx)
        {
            string name = DbConnection.ExecuteScalar($"SELECT `name` FROM users WHERE `id`={ctx.User.ChatID}").ToString()!;
            string response = $"Здравствуйте, {name}!\n\n";

            response += "У вас 0 задач на сегодня\n\n"; // TODO: Уведомления на текущий день

            response += $"/events - Посмотреть все события\n/newEvent - Создать новое событие"; // TODO: Сделать это в виде кнопок
            await ctx.RespondAsync(response);
        }

        [Command]
        public static async Task Events(CommandContext ctx, int page)
        {
            if (page < 1 || page > 255)
            {
                await ctx.RespondAsync("Неверно указана страница!");
                return;
            }
            int offset = (page - 1) * 10;
            List<string?[]> table = DbConnection.ExecuteReader($"SELECT * FROM `reminders` WHERE `owner`={ctx.User.ChatID} LIMIT 10 OFFSET {offset}");
            string response = $"Ваши напоминания | Страница {page} \n";
            offset++;
            foreach (string?[] reminder in table)
            {
                //            {Счётчик}.   {Заголовок}  - {Описание}
                response += $"{offset++}. {reminder[1]} - {reminder[2]}\n";
            }
            response += "\n/back - Вернуться в меню";
            await ctx.RespondAsync(response);
        }

        [Command]
        public static async Task Events(CommandContext ctx)
        {
            await Events(ctx, 1);
        }

        [Command]
        public static async Task NewEvent(CommandContext ctx)
        {
            // 1. Название
            await ctx.RespondAsync("**Придумай заголовок к событию.**\n\nХороший заголовок - очень краткое описание того, что ты хочешь сделать" +
                "Например, \"Сходить к стоматологу\" или \"Купить корм собаке\". Максимум - 64 символа!");
            string eventTitle = await ctx.WaitForUserInput();
            while (eventTitle.Length <= 3 || eventTitle.Length > 64)
            {
                await ctx.RespondAsync($"Заголовок слишком {(eventTitle.Length <= 3 ? "короткий" : "длинный")}! \n" +
                    $"Придумай другой или используй /cancel для отмены");
                eventTitle = await ctx.WaitForUserInput();
            }

            // 2. Описание
            await ctx.RespondAsync("Отлично! Теперь можешь вписать **описание** этого события.\n\n" +
                "Включи сюда любую информацию, что посчитаешь нужной\n" +
                "Лимит - 4096 символов (полное сообщение!)\n" +
                "Или пропусти этот шаг с помощью команды /skip");
            string eventDesc = await ctx.WaitForUserInput();

            // 3. Напоминание
            await ctx.RespondAsync("Если тебе нужно напомнить об этом событии, укажи время и дату, когда это сделать!\n\n" +
                "Формат - дд.мм.гг чч:мм\n" +
                "Или пропусти этот шаг с помощью команды /skip");
            string eventDT = await ctx.WaitForUserInput();
            DateTime eventParsedDT = DateTime.MaxValue;
            if (eventDT != "skip")
            {
                while (!DateTime.TryParseExact(eventDT.Trim(), "dd.MM.yy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out eventParsedDT))
                {
                    await ctx.RespondAsync("Похоже что ты ошибся при вводе даты/времени. К примеру, вот текущие:\n" +
                        $"{DateTime.Now:dd.MM.yy HH:mm}\n\n" +
                        "Используй /skip чтобы не записывать напоминание");
                    eventDT = await ctx.WaitForUserInput();
                }
            }

            // X. Запись
            DbConnection.ExecuteNonQuery($"INSERT INTO `schedule`.`reminders` (`title`, `description`, `owner`, `datedue`) VALUES (@TITLE, @DESC, {ctx.User.ChatID}, @DATE);",
                new() { { "@TITLE", eventTitle },
                        { "@DESC", eventDesc },
                        { "@DATE", eventParsedDT } });
            await ctx.RespondAsync("Событие успешно записано!");
        }
        #endregion

        #region Navigation commands
        //[Command]
        // TODO: Заставить это работать
        public static async Task Skip(CommandContext ctx)
        {
            ctx.User.CancelPendingInput();
            await Task.CompletedTask;
        }
        #endregion

        #region Misc commands
        [Command]
        public static async Task Help(CommandContext ctx)
        {
            await ctx.RespondAsync("Список команд ещё не готов к использованию");
            // await ctx.RespondAsync("⚠ There is no help. **START RUNNING.**");
            // https://i.imgur.com/8gJSSGr.png
        }

        [Command]
        public static async Task Stop(CommandContext ctx)
        {
            DbConnection.ExecuteNonQuery($"DELETE FROM users WHERE `id`={ctx.User.ChatID}");
            await Task.CompletedTask;
        }
        #endregion
    }
}