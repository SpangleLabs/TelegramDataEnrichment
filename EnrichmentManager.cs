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
            Logger.LogWarn($"Callback handler not implemented. Args: {eventArgs}");
        }

        public void HandleText(MessageEventArgs eventArgs)
        {
            var msg = eventArgs.msg;
            if(!Utilities.isBotOwner(msg.from))
            {
                Methods.sendReply(msg.chat.id, msg.message_id, "Sorry this bot is not available for general use.");
                return;
            }
            if (eventArgs.msg.text == "/menu")
            {
                var activeSessions = _sessions.FindAll(s => s.IsActive()).Count;
                Methods.sendReply(
                    msg.chat.id, 
                    msg.message_id,
                    $"Welcome to the enrichment system menu. There are {_sessions.Count} configured sessions, and {activeSessions} are active.",
                    keyboard: MenuKeyboard()
                    );
            }
        }

        private static InlineKeyboardMarkup MenuKeyboard()
        {
            InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("Create new session", "session_create", 0);
            keyboard.addCallbackButton("Start session", "session_start", 1);
            keyboard.addCallbackButton("End session", "session_end", 2);
            keyboard.addCallbackButton("Delete session", "session_delete", 3);
            return keyboard;
        }
    }
}