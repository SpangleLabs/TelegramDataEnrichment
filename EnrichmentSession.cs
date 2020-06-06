namespace TelegramDataEnrichment
{
    public class EnrichmentSession
    {
        public bool IsActive { get; set; }
        public string Name { get; }
        public int Id { get;  }

        public EnrichmentSession(int id, string name)
        {
            Id = id; 
            Name = name;  // User friendly name
            IsActive = false;
        }
    }
}