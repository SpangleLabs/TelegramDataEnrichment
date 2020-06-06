namespace TelegramDataEnrichment
{
    public class EnrichmentSession
    {
        public bool IsActive { get; set; }
        public string Name { get; }
        public string Id { get;  }

        public EnrichmentSession(string id, string name)
        {
            Id = id;  // ID should be a unique string, up to (64-len("/session_start ")=49 characters 
            Name = name;  // User friendly name
            IsActive = false;
        }
    }
}