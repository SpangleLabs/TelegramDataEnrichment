using System.Collections.Generic;
using DreadBot;

namespace TelegramDataEnrichment
{
    public class EnrichmentManager
    {
        private readonly List<EnrichmentSession> _sessions = new List<EnrichmentSession>();
        private PartialSession _partialSession;

        public EnrichmentManager()
        {
            Logger.LogWarn("Created enrichment manager");
            _partialSession = null; // TODO: Load from config
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
                case RootMenu.CallbackName:
                    menu = new RootMenu(_sessions, _partialSession != null);
                    break;
                case StartSessionMenu.CallbackName:
                    menu = new StartSessionMenu(_sessions);
                    break;
                case CreateSessionMenu.CallbackName:
                    _partialSession = new PartialSession();
                    menu = new CreateSessionMenu();
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

            Menu menu = null;
            switch (eventArgs.msg.text)
            {
                case "/menu":
                {
                    menu = new RootMenu(_sessions, _partialSession != null);
                    _partialSession = null;
                    break;
                }
            }

            if (menu == null && _partialSession == null)
            {
                    menu = new UnknownMenu(eventArgs.msg.text);
            }

            if (_partialSession != null && _partialSession.WaitingForText())
            {
                _partialSession.AddText(eventArgs.msg.text);
                if (_partialSession.NextPart() == PartialSession.SessionParts.Done)
                {
                    var newSession = _partialSession.BuildSession("example_id");
                    _sessions.Add(newSession);
                }

                menu = _partialSession.NextMenu();
            }

            menu?.SendReply(eventArgs.msg.chat.id, eventArgs.msg.message_id);
        }
    }
}