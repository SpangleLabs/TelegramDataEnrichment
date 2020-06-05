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
            if (!Utilities.isBotOwner(eventArgs.callbackQuery.from))
            {
                Methods.sendReply(eventArgs.callbackQuery.message.chat.id, eventArgs.callbackQuery.message.message_id, "Sorry this bot is not available for general use.");
                return;
            }

            if (eventArgs.callbackQuery.data == "session_start")
            {
                Methods.answerCallbackQuery(eventArgs.callbackQuery.id);
                this.StartSessionMenu(eventArgs.callbackQuery);
                return;
            }

            Methods.answerCallbackQuery(eventArgs.callbackQuery.id);
            Methods.sendMessage(eventArgs.callbackQuery.message.chat.id, $"I do not understand this callback data yet: {eventArgs.callbackQuery.data}");
        }

        public void HandleText(MessageEventArgs eventArgs)
        {
            var msg = eventArgs.msg;
            if (!Utilities.isBotOwner(msg.from))
            {
                Methods.sendReply(msg.chat.id, msg.message_id, "Sorry this bot is not available for general use.");
                return;
            }

            if (eventArgs.msg.text == "/menu")
            {
                SendRootMenu(eventArgs.msg);
            }
        }

        private void SendRootMenu(Message msg)
        {
            var activeSessions = _sessions.FindAll(s => s.IsActive).Count;
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("Create new session", "session_create", 0);
            keyboard.addCallbackButton("Start session", "session_start", 1);
            keyboard.addCallbackButton("End session", "session_end", 2);
            keyboard.addCallbackButton("Delete session", "session_delete", 3);
            Methods.sendReply(
                msg.chat.id,
                msg.message_id,
                $"Welcome to the enrichment system menu. There are {_sessions.Count} configured sessions, and {activeSessions} are active.",
                keyboard: keyboard
            );
        }

        private void StartSessionMenu(CallbackQuery callback)
        {
            var inActiveSessions = _sessions.FindAll(s => !s.IsActive);
            var keyboard = new InlineKeyboardMarkup();
            var row = 0;
            foreach (var session in inActiveSessions)
            {
                keyboard.addCallbackButton(session.Name, $"session_start {session.Name}", row++);
            }
            keyboard.addCallbackButton("🔙", "menu", row);

            Methods.editMessageText(
                callback.from.id,
                callback.message.message_id,
                "Which enrichment session would you like to start?",
                keyboard: BackOnly("menu")
            );
        }

        private static InlineKeyboardMarkup BackOnly(string callbackData)
        {
            var keyboard = new InlineKeyboardMarkup();
            keyboard.addCallbackButton("🔙", callbackData, 0);
            return keyboard;
        }
    }
}