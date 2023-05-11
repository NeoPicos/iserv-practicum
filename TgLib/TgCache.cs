using System.Timers;
using Telegram.Bot;

namespace TgLib
{
    internal class TgCache
    {
        private static readonly List<TgUser> cache = new();
        private static ITelegramBotClient botClient = null!;
        private static readonly System.Timers.Timer autoclearTimer = new(TimeSpan.FromMinutes(1));

        public static void Initialize(ITelegramBotClient client)
        {
            botClient = client;

            autoclearTimer.Elapsed += Autoclear;
            autoclearTimer.AutoReset = true;
            autoclearTimer.Start();
        }

        public static TgUser GetOrCreateSession(long chatid)
        {
            TgUser? session = cache.FirstOrDefault((x) => x.ChatID == chatid);
            if (session is null)
            {
                session = new TgUser(botClient, chatid);
                cache.Add(session);
            }
            return session;
        }

        public static void DeleteSession(TgUser client)
        {
            UncacheSession(client);
        }

        private static void Autoclear(object? sender, ElapsedEventArgs e)
        {
            foreach (TgUser i in cache)
            {
                if ((DateTime.Now - i.LastMessage?.Date) > TimeSpan.FromMinutes(10))
                {
                    UncacheSession(i);
                }
            }
        }

        private static void UncacheSession(TgUser session)
        {
            cache.Remove(session);
            if (session.PendingInput)
            {
                session.PendingInput = false;
                _ = session.SendMessage("🚫 Действие отменено");
            }
            session.Dispose();
        }
    }
}
