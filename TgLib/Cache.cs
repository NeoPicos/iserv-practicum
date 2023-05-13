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

        public TgUser GetOrCreateUser(long chatid)
        {
            TgUser? user = cache.FirstOrDefault((x) => x.ChatID == chatid);
            if (user is null)
            {
                user = new TgUser(client, chatid);
                cache.Add(user);
            }
            return user;
        }

        public void DeleteUser(TgUser client)
        {
            UncacheUser(client);
        }

        private void Autoclear(object? sender, ElapsedEventArgs e)
        {
            foreach (TgUser i in cache)
            {
                if ((DateTime.Now - i.LastMessage?.Date) > TimeSpan.FromMinutes(10))
                {
                    UncacheUser(i);
                }
            }
        }

        private void UncacheUser(TgUser user)
        {
            cache.Remove(user);
            client.interact.DeleteRequest(user);
            user.Dispose();
        }
    }
}
