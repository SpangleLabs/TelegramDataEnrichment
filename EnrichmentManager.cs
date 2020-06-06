using System.Collections.Generic;
using DreadBot;

namespace TelegramDataEnrichment
{
    public class EnrichmentManager
    {
        private List<EnrichmentSession> _sessions = new List<EnrichmentSession>();

        public EnrichmentManager()
        {
            Logger.LogWarn("Created enrichment manager");
        }

        public void HandleCallback(CallbackEventArgs eventArgs)
        {
            Logger.LogDebug($"Callback handler: {eventArgs.callbackQuery.data}");
            if (!Utilities.isBotOwner(eventArgs.callbackQuery.from))
            {
                Methods.sendReply(eventArgs.callbackQuery.message.chat.id, eventArgs.callbackQuery.message.message_id, "Sorry this bot is not available for general use.");
                return;
            }
            
            Methods.answerCallbackQuery(eventArgs.callbackQuery.id);
            Menu menu;
            switch (eventArgs.callbackQuery.data)
            {
                case StartSessionMenu.CallbackName:
                    menu = new StartSessionMenu(_sessions);
                    break;
                case RootMenu.CallbackName:
                    menu = new RootMenu(_sessions);
                    break;
                default:
                    menu = new UnknownMenu(eventArgs.callbackQuery.data);
                    break;
            }
            menu.EditMessage(
                eventArgs.callbackQuery.message.chat.id, 
                eventArgs.callbackQuery.message.message_id
            );
        }

        public void HandleText(MessageEventArgs eventArgs)
        {
            var msg = eventArgs.msg;
            if (!Utilities.isBotOwner(msg.from))
            {
                Methods.sendReply(msg.chat.id, msg.message_id, "Sorry this bot is not available for general use.");
                return;
            }

            Menu menu;
            switch (eventArgs.msg.text)
            {
                case "/menu":
                {
                    menu = new RootMenu(_sessions);
                    break;
                }
                case "/session_start":
                {
                    menu = new StartSessionMenu(_sessions);
                    break;
                }
                default:
                {
                    menu = new UnknownMenu(eventArgs.msg.text);
                    break;
                }
            } 
            menu.SendReply(eventArgs.msg.chat.id, eventArgs.msg.message_id);
        }
    }
}