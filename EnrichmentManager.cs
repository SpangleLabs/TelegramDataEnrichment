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

        private abstract class Menu
        {
            protected abstract string Text();
            protected abstract InlineKeyboardMarkup Keyboard();

            public void SendReply(long chatId, long messageId)
            {
                Methods.sendReply(chatId, messageId, Text(), keyboard: Keyboard());
            }

            public void EditMessage(long chatId, long messageId)
            {
                Methods.editMessageText(chatId, messageId, Text(), keyboard: Keyboard());
            }
        }

        private class UnknownMenu : Menu
        {
            private readonly string _callbackData;

            public UnknownMenu(string callbackData)
            {
                _callbackData = callbackData;
            }
            
            protected override string Text()
            {
                return $"I do not understand this callback data yet: {_callbackData}";
            }

            protected override InlineKeyboardMarkup Keyboard()
            {
                var keyboard = new InlineKeyboardMarkup();
                keyboard.addCallbackButton("🔙", RootMenu.CallbackName, 0);
                return keyboard;
            }
        }

        private class RootMenu : Menu
        {
            private readonly List<EnrichmentSession> _sessions;
            public const string CallbackName = "menu";

            public RootMenu(List<EnrichmentSession> sessions)
            {
                _sessions = sessions;
            }

            protected override string Text()
            {
                var activeSessions = _sessions.FindAll(s => s.IsActive).Count;
                return "Welcome to the enrichment system menu. " +
                       $"There are {_sessions.Count} configured sessions, and {activeSessions} are active.";
            }

            protected override InlineKeyboardMarkup Keyboard()
            {
                var keyboard = new InlineKeyboardMarkup();
                keyboard.addCallbackButton("Create new session", "session_create", 0);
                keyboard.addCallbackButton("Start session", "session_start", 1);
                keyboard.addCallbackButton("End session", "session_end", 2);
                keyboard.addCallbackButton("Delete session", "session_delete", 3);
                return keyboard;
            }
        }

        private class StartSessionMenu : Menu
        {
            private readonly List<EnrichmentSession> _sessions;
            public const string CallbackName = "session_start";

            public StartSessionMenu(List<EnrichmentSession> sessions)
            {
                _sessions = sessions;
            }

            protected override string Text()
            {
                return "Which enrichment session would you like to start?";
            }

            protected override InlineKeyboardMarkup Keyboard()
            {
                var inActiveSessions = _sessions.FindAll(s => !s.IsActive);
                var keyboard = new InlineKeyboardMarkup();
                var row = 0;
                foreach (var session in inActiveSessions)
                {
                    keyboard.addCallbackButton(session.Name, $"session_start {session.Name}", row++);
                }
                keyboard.addCallbackButton("🔙", RootMenu.CallbackName, row);
                return keyboard;
            }
        }
    }
}