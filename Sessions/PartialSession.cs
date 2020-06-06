using System;

namespace TelegramDataEnrichment.Sessions
{
    public class PartialSession
    {
        public enum SessionParts
        {
            Name,
            Done
        }

        public readonly int Id = 1;
        private string Name { get; set; }

        public PartialSession()
        {
            
        }

        public PartialSession(PartialData data)
        {
            Id = data.Id;
            Name = data.Name;
        }

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

        public PartialData ToData()
        {
            return new PartialData
            {
                Id = Id, 
                Name = Name
            };
        }

        public class PartialData
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}