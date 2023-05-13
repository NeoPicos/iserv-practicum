namespace TgLib
{
    internal class Interactivity
    {
        public readonly Dictionary<TgUser, Request> pendingInputs;

        public Interactivity()
        {
            pendingInputs = new();
        }

        public bool TryGetRequest(TgUser user, out Request request)
        {
            return pendingInputs.TryGetValue(user, out request);
        }

        public Request AddRequest(TgUser user)
        {
            if (TryGetRequest(user, out Request value))
            {
                value.Tcs.SetCanceled();
                pendingInputs.Remove(user);
            }
            Request req = new(user);
            pendingInputs.Add(user, req);
            return req;
        }

        public void SetCompleted(TgUser user, string result)
        {
            if (TryGetRequest(user, out Request req))
            {
                req!.Tcs.SetResult(result);
                DeleteRequest(user);
            }
        }

        public void DeleteRequest(TgUser user, bool setEmpty = false)
        {
            if (TryGetRequest(user, out Request req))
            {
                if (setEmpty)
                    req!.Tcs.SetResult("");
                else 
                    req!.Tcs.SetCanceled();
                pendingInputs.Remove(user);
            }
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
