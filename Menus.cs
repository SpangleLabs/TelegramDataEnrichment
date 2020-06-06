using System.Collections.Generic;
using DreadBot;

namespace TelegramDataEnrichment
{
    internal abstract class Menu
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
                    keyboard.addCallbackButton(session.Name, $"session_start {session.Name}", row++);
                }
                keyboard.addCallbackButton("🔙", RootMenu.CallbackName, row);
                return keyboard;
            }
        }
}