namespace TgLib
{
    internal class InteractivityModule
    {
        #region Public fields
        public readonly Dictionary<long, Request> PendingInputs;
        #endregion

        #region Public methods
        public InteractivityModule()
        {
            PendingInputs = new();
        }

        public bool TryGetRequest(TgUser user, out Request? request)
        {
            return PendingInputs.TryGetValue(user.ChatID, out request);
        }

        public Request AddRequest(TgUser user)
        {
            PendingInputs.Remove(user.ChatID);
            Request req = new(user);
            PendingInputs.Add(user.ChatID, req);
            return req;
        }

        public void SetCompleted(TgUser user, string result)
        {
            if (TryGetRequest(user, out Request? req))
            {
                DeleteRequest(user);
                req!.Tcs.SetResult(result);
            }
        }

        public void DeleteRequest(TgUser user)
        {
            if(PendingInputs.TryGetValue(user.ChatID, out Request? value))
            {
                value.Tcs.SetCanceled();
                PendingInputs.Remove(user.ChatID);
            }
        }
        #endregion
    }

    internal class Request
    {
        public TgUser User;
        public TaskCompletionSource<string> Tcs;

        public Request(TgUser user)
        {
            User = user;
            Tcs = new TaskCompletionSource<string>();
        }
    }
}
