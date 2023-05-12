using System.Timers;

namespace TgLib
{
    internal class UserCache
    {
        private readonly List<TgUser> cache = new();
        private readonly TgBot client = null!;
        private readonly System.Timers.Timer autoclearTimer = new(TimeSpan.FromMinutes(1));

        public UserCache(TgBot client)
        {
            this.client = client;

            autoclearTimer.Elapsed += Autoclear;
            autoclearTimer.AutoReset = true;
            autoclearTimer.Start();
        }

        public TgUser GetOrCreateSession(long chatid)
        {
            TgUser? session = cache.FirstOrDefault((x) => x.ChatID == chatid);
            if (session is null)
            {
                session = new TgUser(client, chatid);
                cache.Add(session);
            }
            return session;
        }

        public void DeleteSession(TgUser client)
        {
            UncacheSession(client);
        }

        private void Autoclear(object? sender, ElapsedEventArgs e)
        {
            foreach (TgUser i in cache)
            {
                if ((DateTime.Now - i.LastMessage?.Date) > TimeSpan.FromMinutes(10))
                {
                    UncacheSession(i);
                }
            }
        }

        private void UncacheSession(TgUser user)
        {
            cache.Remove(user);
            client.interact.DeleteRequest(user);
            user.Dispose();
        }
    }
}
