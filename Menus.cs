using System.Collections.Generic;
using DreadBot;

namespace TelegramDataEnrichment
{
    public abstract class Menu
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

    internal class UnknownMenu : Menu
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

    internal class RootMenu : Menu
        {
            private readonly List<EnrichmentSession> _sessions;
            private readonly bool _creationInProgress;
            public const string CallbackName = "menu";

            public RootMenu(List<EnrichmentSession> sessions, bool creationInProgress)
            {
                _sessions = sessions;
                _creationInProgress = creationInProgress;
            }

            protected override string Text()
            {
                var activeSessions = _sessions.FindAll(s => s.IsActive).Count;
                var text = "Welcome to the enrichment system menu.\n";
                if (_creationInProgress)
                {
                    text += "Cancelled session creation.\n";
                }
                text += $"There are {_sessions.Count} configured sessions, and {activeSessions} are active.";
                return text;
            }

            protected override InlineKeyboardMarkup Keyboard()
            {
                var keyboard = new InlineKeyboardMarkup();
                keyboard.addCallbackButton("Create new session", CreateSessionMenu.CallbackName, 0);
                keyboard.addCallbackButton("Start session", StartSessionMenu.CallbackName, 1);
                keyboard.addCallbackButton("End session", "session_end", 2);
                keyboard.addCallbackButton("Delete session", "session_delete", 3);
                return keyboard;
            }
        }

    internal class StartSessionMenu : Menu
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
                    keyboard.addCallbackButton(session.Name, $"session_start {session.Id}", row++);
                }
                keyboard.addCallbackButton("🔙", RootMenu.CallbackName, row);
                return keyboard;
            }
        }

    internal class CreateSessionMenu : Menu
    {
        public const string CallbackName = "session_create";
        protected override string Text()
        {
            return "Creating a new session.\nWhat would you like to name the session?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            return null;
        }
    }

    internal class SessionCreatedMenu : Menu
    {
        protected override string Text()
        {
            return "New session has been created: "; // TODO: add new session info
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            return null;
        }
    }
}