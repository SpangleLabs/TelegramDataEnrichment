using System;
using System.Collections.Generic;
using DreadBot;
using TelegramDataEnrichment.Sessions;

namespace TelegramDataEnrichment
{
    public abstract class Menu
    {
        protected abstract string Text();
        protected abstract InlineKeyboardMarkup Keyboard();

        public void SendReply(long chatId, long messageId)
        {
            Methods.sendReply(chatId, messageId, Text(), keyboard: Keyboard(), parse_mode: "html");
        }

        public void EditMessage(long chatId, long messageId)
        {
            Methods.editMessageText(chatId, messageId, Text(), keyboard: Keyboard(), parse_mode: "html");
        }

        public void SendMessage(long chatId)
        {
            Methods.sendMessage(chatId, Text(), keyboard: Keyboard(), parse_mode: "html");
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
        private const int BarLength = 10;

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

            text += $"There are {_sessions.Count} configured sessions, and {activeSessions} are active.\n";
            foreach (var session in _sessions)
            {
                var emoji = "";
                if (session.IsActive()) {
                    emoji = "▶️";
                }
                if (session.IsLive()) {
                    emoji = "⏳";
                }
                text += $"- {emoji}{session.Name}:\n";
                var doneData = session.CompletedData().Count;
                var allData = session.AllData().Count;
                var percentage = (double)doneData / allData;
                var progress = (doneData * BarLength) / allData;
                var progressBar = new string('█', progress) + new string('_', BarLength - progress);
                text += $"<pre>[{progressBar}] {percentage:P0} ({doneData}/{allData})</pre>\n";
            }
            return text;
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("Create new session", CreateSessionMenu.CallbackName, 0);
            keyboard.addCallbackButton("Start session", StartSessionMenu.CallbackName, 1);
            keyboard.addCallbackButton("End session", StopSessionMenu.CallbackName, 2);
            keyboard.addCallbackButton("Delete session", DeleteSessionMenu.CallBackName, 3);
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
            var inactiveSessions = _sessions.FindAll(s => !s.IsActive && s.IncompleteData().Count > 0);
            var keyboard = new InlineKeyboardMarkup();
            var row = 0;
            foreach (var session in inactiveSessions)
            {
                keyboard.addCallbackButton(session.Name, $"{CallbackName}:{session.Id}", row++);
            }

            keyboard.addCallbackButton("🔙", RootMenu.CallbackName, row);
            return keyboard;
        }
    }

    internal class SessionStartedMenu : Menu
    {
        protected override string Text()
        {
            return "Session started.";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            return null;
        }
    }

    internal class NoMatchingSessionMenu : Menu
    {
        private readonly string _sessionId;

        public NoMatchingSessionMenu(string sessionId)
        {
            _sessionId = sessionId;
        }

        protected override string Text()
        {
            return $"No session matching the id: {_sessionId}";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("🔙 to menu", RootMenu.CallbackName, 0);
            return keyboard;
        }
    }

    internal class StopSessionMenu : Menu
    {
        private readonly List<EnrichmentSession> _sessions;
        public const string CallbackName = "stop_session";

        public StopSessionMenu(List<EnrichmentSession> sessions)
        {
            _sessions = sessions;
        }

        protected override string Text()
        {
            return "Which enrichment session would you like to stop?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var activeSessions = _sessions.FindAll(s => s.IsActive);
            var keyboard = new InlineKeyboardMarkup();
            var row = 0;
            foreach (var session in activeSessions)
            {
                keyboard.addCallbackButton(session.Name, $"{CallbackName}:{session.Id}", row++);
            }

            keyboard.addCallbackButton("🔙", RootMenu.CallbackName, row);
            return keyboard;
        }
    }

    internal class SessionStoppedMenu : Menu
    {
        protected override string Text()
        {
            return "Session ended.";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("🔙 to menu", RootMenu.CallbackName, 0);
            return keyboard;
        }
    }

    internal class SessionCompleteMenu : Menu
    {
        private readonly EnrichmentSession _session;
        public const string LiveSessionCallBackName = "stay_active";
        
        public SessionCompleteMenu(EnrichmentSession session)
        {
            _session = session;
        }
        
        protected override string Text()
        {
            return "Enrichment session complete!\nWould you like to leave it active for new results?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("Keep active", $"{LiveSessionCallBackName}:{_session.Id}", 0);
            keyboard.addCallbackButton("End session", $"{StopSessionMenu.CallbackName}:{_session.Id}", 1);
            return keyboard;
        }
    }

    internal class SessionCompleteLiveMenu : Menu
    {
        protected override string Text()
        {
            return "Session complete, and watching for updates";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            return null;
        }
    }

    internal class DeleteSessionMenu : Menu
    {
        private readonly List<EnrichmentSession> _sessions;
        public const string CallBackName = "session_delete";

        public DeleteSessionMenu(List<EnrichmentSession> sessions)
        {
            _sessions = sessions;
        }

        protected override string Text()
        {
            return "Which session did you want to delete?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            var row = 0;
            foreach (var session in _sessions)
            {
                keyboard.addCallbackButton(session.Name, $"{CallBackName}:{session.Id}", row++);
            }

            keyboard.addCallbackButton("🔙 to menu", RootMenu.CallbackName, row);
            return keyboard;
        }
    }

    internal class DeleteSessionConfirmMenu : Menu
    {
        private readonly EnrichmentSession _session;

        public DeleteSessionConfirmMenu(EnrichmentSession session)
        {
            _session = session;
        }

        protected override string Text()
        {
            return $"Are you sure you want to delete the session: {_session.Name}?";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton(
                "Yes, delete the session.",
                $"{DeleteSessionConfirmedMenu.CallbackName}:{_session.Id}",
                0
            );
            keyboard.addCallbackButton("No, go back to menu 🔙", RootMenu.CallbackName, 1);
            return keyboard;
        }
    }

    internal class DeleteSessionConfirmedMenu : Menu
    {
        public const string CallbackName = "session_delete_conf";

        protected override string Text()
        {
            return "Deleted session.";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("🔙 to menu", RootMenu.CallbackName, 0);
            return keyboard;
        }
    }

    public class AddedNewSessionOption : Menu
    {
        private EnrichmentSession _session;
        private string _newOption;

        public AddedNewSessionOption(EnrichmentSession session, string newOption)
        {
            _session = session;
            _newOption = newOption;
        }
        protected override string Text()
        {
            return $"Added new option \"{_newOption}\" to enrichment session: {_session.Name}";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            return null;
        }
    }

    public class EnrichmentExceptionMenu : Menu
    {
        private readonly EnrichmentException _ex;
        private readonly bool _withBackButton;

        public EnrichmentExceptionMenu(EnrichmentException ex, bool withBackButton = false)
        {
            _ex = ex;
            _withBackButton = withBackButton;
        }
        protected override string Text()
        {
            return $"An enrichment exception was thrown.\n{_ex}";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            if (!_withBackButton) return null;
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("🔙 to root menu", RootMenu.CallbackName, 0);
            return keyboard;
        }
    }

    public class UnknownExceptionMenu : Menu
    {
        private readonly Exception _ex;
        private readonly bool _withBackButton;

        public UnknownExceptionMenu(Exception ex, bool withBackButton = false)
        {
            _ex = ex;
            _withBackButton = withBackButton;
        }
        
        protected override string Text()
        {
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            Logger.LogError($"Error ID{guid}: {_ex}");
            return $"An error occurred. It is viewable in the logs with ID {guid}";
        }

        protected override InlineKeyboardMarkup Keyboard()
        {
            if (!_withBackButton) return null;
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("🔙 to root menu", RootMenu.CallbackName, 0);
            return keyboard;
        }
    }
}
