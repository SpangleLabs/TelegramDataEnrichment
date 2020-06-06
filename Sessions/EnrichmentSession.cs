namespace TelegramDataEnrichment.Sessions
{
    public class EnrichmentSession
    {
        public bool IsActive { get; private set; }
        public string Name { get; }
        public int Id { get; }

        public EnrichmentSession(int id, string name)
        {
            Id = id;
            Name = name; // User friendly name
            IsActive = false;
        }

        public EnrichmentSession(SessionData data)
        {
            Id = data.Id;
            Name = data.Name;
            IsActive = data.IsActive;
        }

        public void Start()
        {
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
        }

        public SessionData ToData()
        {
            return new SessionData
            {
                Id = Id, 
                Name = Name, 
                IsActive = IsActive
            };
        }

        public class SessionData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
        }
    }
}