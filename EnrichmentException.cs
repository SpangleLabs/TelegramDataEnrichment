using System;

namespace TelegramDataEnrichment
{
    public class EnrichmentException : Exception
    {
        protected EnrichmentException(string s) : base(s)
        {
        }
    }
}