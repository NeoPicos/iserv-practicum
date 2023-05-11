using Telegram.Bot.Types;
using Telegram.Bot;

namespace TgLib
{
    public class TgUser : IDisposable, IEquatable<TgUser>
    {
        public long ChatID { get; }
        public bool PendingInput { get; set; } = false;
        public Message? LastMessage { get; set; } = null;

        private readonly ITelegramBotClient client;

        public TgUser(ITelegramBotClient botclient, long id)
        {
            client = botclient;
            ChatID = id;
        }

        // TODO: Прикрепление вложений
        public async Task<Message> SendMessage(string messageText, IEnumerable<MessageEntity>? entites = null)
        {
            try
            {
                return disposed
                    ? throw new ObjectDisposedException(GetType().FullName)
                    : await client.SendTextMessageAsync(ChatID, messageText, entities: entites, cancellationToken: CancellationToken.None);
            }catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return new Message();
        }

        #region IDisposable
        private bool disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region IEquateable
        public override bool Equals(object? obj)
        {
            return Equals(obj as TgUser);
        }

        public bool Equals(TgUser? other)
        {
            return other is not null &&
            ChatID == other.ChatID;
        }

        public static bool operator ==(TgUser? left, TgUser? right)
        {
            return EqualityComparer<TgUser>.Default.Equals(left, right);
        }

        public static bool operator !=(TgUser? left, TgUser? right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(client, ChatID, PendingInput, LastMessage, disposed);
        }
        #endregion
    }
}
