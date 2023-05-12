using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgLib
{
    /// <summary>
    /// Класс, представляющий пользователя и чат с ним
    /// </summary>
    public class TgUser : IDisposable, IEquatable<TgUser>
    {
        #region Public fields
        /// <summary>
        /// ID Telegram чата
        /// </summary>
        public long ChatID { get; }
        /// <summary>
        /// Последнее отправленное/полученное сообщение в этом чате
        /// </summary>
        public Message? LastMessage { get; set; } = null;
        #endregion

        #region Internal fields
        internal readonly ITelegramBotClient client;
        #endregion

        #region Public Methods
        /// <summary>
        /// Создаёт новый экземпляр пользователя, но не чат с ним <para>Использование этого метода напрямую не рекомендуется, используйте <see cref="UserCache.GetOrCreateSession(long)"/></para>
        /// </summary>
        /// <param name="botclient">Клиент бота, с которым связать пользователя</param>
        /// <param name="id">ID чата</param>
        public TgUser(ITelegramBotClient botclient, long id)
        {
            client = botclient;
            ChatID = id;
        }

        // TODO: Прикрепление вложений
        /// <summary>
        /// Отправить сообщение в чат с пользователем
        /// </summary>
        /// <param name="messageText">Текст сообщения</param>
        /// <param name="entites">Приложения к сообщению</param>
        /// <returns>Отправленное сообщение</returns>
        public async Task<Message> SendMessage(string messageText, IEnumerable<MessageEntity>? entites = null)
        {
            try
            {
                return disposed
                    ? throw new ObjectDisposedException(GetType().FullName)
                    : await client.SendTextMessageAsync(ChatID, messageText, entities: entites, cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return new Message();
        }
        #endregion

        #region IDisposable
        private bool disposed;

        /// <inheritdoc cref="IDisposable.Dispose"/>
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region IEquateable
        /// <inheritdoc cref="IEquatable{TgUser}.Equals(TgUser)"/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as TgUser);
        }

        /// <inheritdoc/>
        public bool Equals(TgUser? other)
        {
            return other is not null &&
            ChatID == other.ChatID;
        }

        /// <inheritdoc cref="Equals(TgUser?)"/>
        public static bool operator ==(TgUser? left, TgUser? right)
        {
            return EqualityComparer<TgUser>.Default.Equals(left, right);
        }

        /// <inheritdoc cref="Equals(TgUser?)"/>
        public static bool operator !=(TgUser? left, TgUser? right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(client, ChatID, LastMessage, disposed);
        }
        #endregion
    }
}
