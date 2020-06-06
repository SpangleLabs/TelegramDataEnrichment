﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            var callback = eventArgs.callbackQuery;
            Logger.LogDebug($"Callback handler: {callback.data}");
            if (!Utilities.isBotOwner(callback.from))
            {
                Methods.sendReply(callback.message.chat.id, callback.message.message_id, "Sorry this bot is not available for general use.");
                return;
            }
            
            Methods.answerCallbackQuery(callback.id);
            Menu menu;
            switch (callback.data)
            {
                case RootMenu.CallbackName:
                    menu = new RootMenu(_sessions, _partialSession != null);
                    break;
                case StartSessionMenu.CallbackName:
                    menu = new StartSessionMenu(_sessions);
                    break;
                case StopSessionMenu.CallbackName:
                    menu = new StopSessionMenu(_sessions);
                    break;
                case CreateSessionMenu.CallbackName:
                    _partialSession = new PartialSession();
                    menu = new CreateSessionMenu();
                    break;
                case DeleteSessionMenu.CallBackName:
                    menu = new DeleteSessionMenu(_sessions);
                    break;
                default:
                    menu = new UnknownMenu(callback.data);
                    break;
            }

            if (callback.data.StartsWith(StartSessionMenu.CallbackName + ":"))
            {
                menu = StartSession(callback.data);
            }
            if (callback.data.StartsWith(StopSessionMenu.CallbackName + ":"))
            {
                menu = StopSession(callback.data);
            }
            if (callback.data.StartsWith(DeleteSessionMenu.CallBackName + ":"))
            {
                menu = ConfirmDeleteSession(callback);
            }
            if (callback.data.StartsWith(DeleteSessionConfirmedMenu.CallbackName + ":"))
            {
                menu = DeleteSession(callback);
            }
            menu.EditMessage(
                callback.message.chat.id, 
                callback.message.message_id
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
                    var newSession = _partialSession.BuildSession(NextSessionId());
                    _sessions.Add(newSession);
                    menu = new SessionCreatedMenu(newSession);
                    _partialSession = null;
                }
                else
                {
                    menu = _partialSession.NextMenu();
                }
            }

            menu?.SendReply(eventArgs.msg.chat.id, eventArgs.msg.message_id);
        }
        
        private EnrichmentSession GetSessionById(string sessionId)
        {
            return _sessions.FirstOrDefault(session => session.Id.ToString().Equals(sessionId));
        }

        private string SessionIdFromCallbackData(string callbackData)
        {
            return callbackData.Split(':')[1];
        }

        private Menu StartSession(string callbackData)
        {
            var sessionId = SessionIdFromCallbackData(callbackData);
            var session = GetSessionById(sessionId);
            if (session == null) return new NoMatchingSessionMenu(sessionId);
            session.Start();
            return new SessionStartedMenu();
        }

        private Menu StopSession(string callbackData)
        {
            var sessionId = SessionIdFromCallbackData(callbackData);
            var session = GetSessionById(sessionId);
            if (session == null) return new NoMatchingSessionMenu(sessionId);
            session.Stop();
            return new SessionStoppedMenu();
        }

        private Menu ConfirmDeleteSession(CallbackQuery callback)
        {
            Menu menu;
            var sessionId = SessionIdFromCallbackData(callback.data);
            var session = GetSessionById(sessionId);
            if (session == null)
            {
                menu = new NoMatchingSessionMenu(sessionId);
            }
            else
            {
                menu = new DeleteSessionConfirmMenu(session);
            }
            return menu;
        }
        
        private Menu DeleteSession(CallbackQuery callback)
        {
            Menu menu;
            var sessionId = SessionIdFromCallbackData(callback.data);
            var session = GetSessionById(sessionId);
            if (session == null)
            {
                menu = new NoMatchingSessionMenu(sessionId);
            }
            else
            {
                _sessions.Remove(session);
                menu = new DeleteSessionConfirmedMenu();
            }
            return menu;
        }
        
        private int NextSessionId()
        {
            if (!_sessions.Any())
            {
                return 0;
            }
            var currentIds = _sessions.Select(x => x.Id);
            var takenIds = new BitArray(_sessions.Count, false);
            foreach (var id in currentIds)
            {
                if (id < _sessions.Count) takenIds[id] = true;
            }

            for (var i = 0; i < _sessions.Count; i++)
            {
                if (takenIds[i] == false) return i;
            }

            return _sessions.Count;
        }
    }
}