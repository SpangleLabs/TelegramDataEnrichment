
using System;

namespace TelegramDataEnrichment
{
    public class PartialSession
    {
        public enum SessionParts
        {
            Name,
            Done
        }
        private string Name { get; set; }

        public SessionParts NextPart()
        {
            return Name == null ? SessionParts.Name : SessionParts.Done;
        }

        public Menu NextMenu()
        {
            switch (NextPart())
            {
                case SessionParts.Name:
                    return new CreateSessionMenu();
                case SessionParts.Done:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        public bool WaitingForText()
        {
            return NextPart() == SessionParts.Name;
        }

        public void AddText(string text)
        {
            if (NextPart() == SessionParts.Name)
            {
                Name = text;
            }
        }

        public EnrichmentSession BuildSession(int nextId)
        {
            return new EnrichmentSession(nextId, Name);
        }
    }
}