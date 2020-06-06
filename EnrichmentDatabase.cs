using System.Collections.Generic;
using System.Linq;
using DreadBot;
using LiteDB;
using Logger = DreadBot.Logger;

namespace TelegramDataEnrichment
{
    public class EnrichmentDatabase
    {
        private readonly LiteCollection<EnrichmentSession.SessionData> _sessions;
        private readonly LiteCollection<PartialSession.PartialData> _partialSessions;

        internal EnrichmentDatabase()
        {
            Logger.LogInfo("Loading enrichment sessions from database...");
            _sessions = Database.GetCollection<EnrichmentSession.SessionData>("EnrichmentSessions");
            _partialSessions = Database.GetCollection<PartialSession.PartialData>("EnrichmentPartial");
            
            Logger.LogInfo(_sessions.Count(Query.All()) + " enrichment sessions loaded.");
            Logger.LogInfo(_partialSessions.Count(Query.All()) + " partial session loaded.");
        }

        public List<EnrichmentSession> ListSessions()
        {
            lock (_sessions)
            {
                return _sessions.FindAll()
                    .Select(sess => new EnrichmentSession(sess))
                    .ToList();
            }
        }

        public void SaveSession(EnrichmentSession session)
        {
            lock (_sessions)
            {
                _sessions.Upsert(session.ToData());
            }
        }

        public void RemoveSession(EnrichmentSession session)
        {
            lock (_sessions)
            {
                _sessions.Delete(session.Id);
            }
        }
        
        public List<PartialSession> ListPartials()
        {
            lock (_partialSessions)
            {
                return _partialSessions.FindAll()
                    .Select(data => new PartialSession(data))
                    .ToList();
            }
        }

        public void SavePartial(PartialSession partial)
        {
            lock (_partialSessions)
            {
                _partialSessions.Upsert(partial.ToData());
            }
        }

        public void RemovePartial(PartialSession partial)
        {
            lock (_partialSessions)
            {
                _partialSessions.Delete(partial.Id);
            }
        }
    }
}