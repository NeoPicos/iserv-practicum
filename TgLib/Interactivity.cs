namespace TgLib
{
    internal class Interactivity
    {
        public readonly Dictionary<TgUser, Request> pendingInputs;

        public Interactivity()
        {
            pendingInputs = new();
        }

        public Request AddRequest(TgUser user)
        {
            if (pendingInputs.TryGetValue(user, out Request value))
            {
                value.Tcs.SetCanceled();
                pendingInputs.Remove(user);
            }
            Request req = new(user);
            pendingInputs.Add(user, req);
            return req;
        }

        public bool TryGetRequest(TgUser user, out Request? req)
        {
            req = pendingInputs.FirstOrDefault((x) => x.Key == user).Value;
            return req != null;
        }

        public void SetCompleted(TgUser user, string result)
        {
            pendingInputs[user].Tcs.SetResult(result);
            DeleteRequest(user);
        }

        public void DeleteRequest(TgUser user)
        {
            pendingInputs.Remove(user);
        }
    }

    internal struct Request
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
