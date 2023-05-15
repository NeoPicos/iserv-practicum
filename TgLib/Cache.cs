using System.Timers;

namespace TgLib
{
    internal class CacheModule
    {
        #region Private fields
        private readonly List<TgUser> _cache = new();
        private readonly TgBot _client = null!;
        private readonly System.Timers.Timer _autoclearTimer = new(TimeSpan.FromMinutes(10));
        private readonly TimeSpan _autoremoveInterval = TimeSpan.FromMinutes(10);
        #endregion

        #region Public methods
        public CacheModule(TgBot client, TimeSpan? AutoremoveInterval=null)
        {
            _client = client;

            _autoremoveInterval = AutoremoveInterval ?? TimeSpan.FromMinutes(10);
            _autoclearTimer.Elapsed += Autoclear;
            _autoclearTimer.AutoReset = true;
        }

        public TgUser GetOrCreateUser(long chatid)
        {
            TgUser? user = _cache.FirstOrDefault((x) => x.ChatID == chatid);
            if (user is null)
            {
                user = new TgUser(_client, chatid);
                _cache.Add(user);
            }
            return user;
        }

        public void UncacheUser(TgUser user)
        {
            _cache.Remove(user);
            _client._interactivity.DeleteRequest(user);
            user.Dispose();
        }

        public void AutoclearSetRunning(bool enabled)
        {
            if(enabled)
                _autoclearTimer.Start();
            else 
                _autoclearTimer.Stop();
        }
        #endregion

        #region Private methods
        private void Autoclear(object? sender, ElapsedEventArgs e)
        {
            foreach (TgUser i in _cache)
            {
                if ((DateTime.Now - i.LastMessage?.Date) > _autoremoveInterval)
                {
                    UncacheUser(i);
                }
            }
        }
        #endregion
    }
}
