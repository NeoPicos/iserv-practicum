using Telegram.Bot.Types;
using TgLib;

namespace Practicum
{
    internal static class Callbacks
    {
        // TODO: Реализовать обработку запросов в TgBot.
        internal static async Task CallbackQueryRecieved(TgBot sender, TgUser user, CallbackQuery query)
        {
            switch (query.Data)
            {
                case "events":
                    sender.InvokeCommand("events", user, new());
                    break;
                case "newEvent":
                    sender.InvokeCommand("newEvent", user, new());
                    break;
                case "menu":
                    sender.InvokeCommand("menu", user, new());
                    break;
                case "left":
                    {
                        Message msg = query.Message!;
                        string text = msg.Text!;
                        int openBrace = text.IndexOf('[') + 1;
                        int page = int.Parse(text[openBrace..text.IndexOf(']')]);
                        if (page-- < 1)
                            page = 1;
                        sender.InvokeCommand("events", user, new() { page.ToString() });
                        break;
                    }
                case "right":
                    {
                        Message msg = query.Message!;
                        string text = msg.Text!;
                        int openBrace = text.IndexOf('[') + 1;
                        int page = int.Parse(text[openBrace..text.IndexOf(']')]);
                        if (page++ > 255)
                            page = 255;
                        sender.InvokeCommand("events", user, new() { page.ToString() });
                        break;
                    }
            }
            await Task.CompletedTask;
        }
    }
}
