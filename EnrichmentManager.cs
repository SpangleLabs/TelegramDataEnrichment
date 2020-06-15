using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DreadBot;
using TelegramDataEnrichment.Sessions;

namespace TelegramDataEnrichment
{
    public class EnrichmentManager
    {
        private readonly EnrichmentDatabase _database;
        private readonly List<EnrichmentSession> _sessions;
        private PartialSession _partialSession;

        public EnrichmentManager(EnrichmentDatabase database)
        {
            Logger.LogWarn("Created enrichment manager");
            _database = database;
            _sessions = database.ListSessions();
            _partialSession = database.ListPartials().FirstOrDefault();
        }

        public void HandleCallback(CallbackEventArgs eventArgs)
        {
            var callback = eventArgs.callbackQuery;
            Logger.LogDebug($"Callback handler: {callback.data}");
            if (!Utilities.isBotOwner(callback.from))
            {
                Methods.sendReply(callback.message.chat.id, callback.message.message_id,
                    "Sorry this bot is not available for general use.");
                return;
            }

            Methods.answerCallbackQuery(callback.id);
            Menu menu = null;
            try
            {
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
                        _partialSession = new PartialSession(callback.message.chat.id);
                        _database.SavePartial(_partialSession);
                        menu = new CreateSessionMenu();
                        break;
                    case DeleteSessionMenu.CallBackName:
                        menu = new DeleteSessionMenu(_sessions);
                        break;
                }

                if (callback.data.StartsWith(StartSessionMenu.CallbackName + ":"))
                {
                    menu = StartSession(callback.data);
                }
                else if (callback.data.StartsWith(StopSessionMenu.CallbackName + ":"))
                {
                    menu = StopSession(callback.data);
                }
                else if (callback.data.StartsWith(DeleteSessionMenu.CallBackName + ":"))
                {
                    menu = ConfirmDeleteSession(callback);
                }
                else if (callback.data.StartsWith(DeleteSessionConfirmedMenu.CallbackName + ":"))
                {
                    menu = DeleteSession(callback);
                }

                if (menu == null && _partialSession != null && _partialSession.WaitingForCallback())
                {
                    _partialSession.AddCallback(callback.data);
                    _database.SavePartial(_partialSession);
                    menu = CheckPartialCompletionAndGetNextMenu();
                }

                if (menu == null)
                {
                    if (callback.data.StartsWith(EnrichmentSession.CallbackName + ":"))
                    {
                        var sessionId = callback.data.Split(':')[1];
                        var matchingSessions =
                            _sessions.Where(s => s.IsActive && s.Id.ToString().Equals(sessionId)).ToList();
                        foreach (var session in matchingSessions)
                        {
                            session.HandleCallback(callback.data);
                            _database.SaveSession(session);
                        }
                    }
                    else
                    {
                        menu = new UnknownMenu(callback.data);
                    }
                }
            }
            catch (EnrichmentException ex)
            {
                menu = new EnrichmentExceptionMenu(ex, true);
            }
            catch (Exception ex)
            {
                menu = new UnknownExceptionMenu(ex, true);
            }

            menu?.EditMessage(
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

            try
            {
                switch (eventArgs.msg.text)
                {
                    case "/menu":
                    {
                        menu = new RootMenu(_sessions, _partialSession != null);
                        _partialSession = null;
                        break;
                    }
                }

                if (menu == null && _partialSession != null && _partialSession.WaitingForText())
                {
                    _partialSession.AddText(eventArgs.msg.text);
                    _database.SavePartial(_partialSession);
                    menu = CheckPartialCompletionAndGetNextMenu();
                }

                if (menu == null)
                {
                    foreach (var session in _sessions)
                    {
                        menu = session.HandleMessage(eventArgs.msg);
                        _database.SaveSession(session);
                        if (menu != null) break;
                    }
                }
            }
            catch (EnrichmentException ex)
            {
                menu = new EnrichmentExceptionMenu(ex);
            }
            catch (Exception ex)
            {
                menu = new UnknownExceptionMenu(ex);
            }

            menu?.SendReply(eventArgs.msg.chat.id, eventArgs.msg.message_id);
        }

        private Menu CheckPartialCompletionAndGetNextMenu()
        {
            if (_partialSession.NextPart() != PartialSession.SessionParts.Done) return _partialSession.NextMenu();
            var newSession = _partialSession.BuildSession(NextSessionId());
            _sessions.Add(newSession);
            _database.SaveSession(newSession);
            _database.RemovePartial(_partialSession);
            _partialSession = null;
            return new SessionCreatedMenu(newSession);
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
            _database.SaveSession(session);
            return new SessionStartedMenu();
        }

        private Menu StopSession(string callbackData)
        {
            var sessionId = SessionIdFromCallbackData(callbackData);
            var session = GetSessionById(sessionId);
            if (session == null) return new NoMatchingSessionMenu(sessionId);
            session.Stop();
            _database.SaveSession(session);
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
                _database.RemoveSession(session);
                menu = new DeleteSessionConfirmedMenu();
            }

            return menu;
        }

        private int NextSessionId()
        {
            if (!_sessions.Any())
            {
                return 1;
            }

            var currentIds = _sessions.Select(x => x.Id);
            var takenIds = new BitArray(_sessions.Count, false);
            foreach (var id in currentIds)
            {
                if (id < _sessions.Count) takenIds[id] = true;
            }

            for (var i = 1; i < _sessions.Count; i++)
            {
                if (takenIds[i] == false) return i;
            }

            return _sessions.Count + 1;
        }
    }
}