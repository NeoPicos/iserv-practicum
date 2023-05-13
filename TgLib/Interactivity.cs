namespace TgLib
{
    internal class Interactivity
    {
        public readonly Dictionary<long, Request> pendingInputs;

        public Interactivity()
        {
            pendingInputs = new();
        }

        public bool TryGetRequest(TgUser user, out Request? request)
        {
            return pendingInputs.TryGetValue(user.ChatID, out request);
        }

        public Request AddRequest(TgUser user)
        {
            pendingInputs.Remove(user.ChatID);
            Request req = new(user);
            pendingInputs.Add(user.ChatID, req);
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
            pendingInputs.Remove(user.ChatID);
        }
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
