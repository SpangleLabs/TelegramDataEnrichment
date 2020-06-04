namespace TelegramDataEnrichment
{
    public class EnrichmentSession
    {
        private bool _isActive;

        public EnrichmentSession()
        {
            _isActive = false;
        }

        public bool IsActive()
        {
            return _isActive;
        }
    }
}