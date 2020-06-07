using System;

namespace TelegramDataEnrichment.Sessions
{
    public class PartialSession
    {
        public enum SessionParts
        {
            Name,
            BatchCount,
            Done
        }

        public readonly int Id = 1;
        private string Name { get; set; }
        private int? BatchCount { get; set; }

        public PartialSession()
        {
        }

        public PartialSession(PartialData data)
        {
            Id = data.Id;
            Name = data.Name;
            BatchCount = data.BatchCount;
        }

        public SessionParts NextPart()
        {
            if (Name == null)
            {
                return SessionParts.Name;
            }

            return BatchCount == null ? SessionParts.BatchCount : SessionParts.Done;
        }

        public Menu NextMenu()
        {
            switch (NextPart())
            {
                case SessionParts.Name:
                    return new CreateSessionMenu();
                case SessionParts.BatchCount:
                    return new CreateSessionBatchSizeMenu();
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

        public bool WaitingForCallback()
        {
            return NextPart() == SessionParts.BatchCount;
        }

        public void AddCallback(string callbackData)
        {
            if (
                NextPart() == SessionParts.BatchCount
                && callbackData.StartsWith($"{CreateSessionBatchSizeMenu.CallbackName}:")
            )
            {
                var batchSize = callbackData.Split(':')[1];
                BatchCount = int.Parse(batchSize);
            }
        }

        public EnrichmentSession BuildSession(int nextId)
        {
            if (Name == null || BatchCount == null)
            {
                throw new ArgumentNullException();
            }

            return new EnrichmentSession(nextId, Name, (int) BatchCount);
        }

        public PartialData ToData()
        {
            return new PartialData
            {
                Id = Id,
                Name = Name,
                BatchCount = BatchCount
            };
        }

        public class PartialData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int? BatchCount { get; set; }
        }
    }
}