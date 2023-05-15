﻿using Telegram.Bot.Types;
using TgLib;

namespace Practicum
{
    internal static class Callbacks
    {
        internal static async Task CallbackQueryRecieved(TgBot sender, TgUser user, CallbackQuery query)
        {
            switch (query.Data)
            {
                case "events":
                case "newEvent":
                case "menu":
                    sender.InvokeCommand(query.Data, user, new());
                    break;
                case "left":
                case "toleft":
                case "right":
                case "toright":
                    ChangePage(sender, user, query);
                    break;
                case "cancel":
                    user.CancelPendingInput(true); 
                    break;
            }
            await Task.CompletedTask;
        }

        internal static void ChangePage(TgBot sender, TgUser user, CallbackQuery query)
        {
            Message msg = query.Message!;
            string text = msg.Text!;
            int page = int.Parse(text[(text.IndexOf('[') + 1)..text.IndexOf('/')]);
            if (query.Data == "left")
                page = Math.Max(page - 1, 1);
            else if (query.Data == "right")
                page = Math.Min(page + 1, 255);
            else
                page = query.Data == "toleft" ? 1 : 255;

            sender.InvokeCommand("events", user, new() { page.ToString() });
        }
    }
}
