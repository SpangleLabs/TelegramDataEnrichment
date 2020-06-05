namespace TelegramDataEnrichment
{
    public class EnrichmentSession
    {
        public bool IsActive { get; set; }
        public string Name { get; }

        public EnrichmentSession(string name)
        {
            Name = name;
            IsActive = false;
        }
    }
}