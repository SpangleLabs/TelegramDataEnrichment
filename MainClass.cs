using System;
using DreadBot;

namespace TelegramDataEnrichment
{
    public class MainClass : IDreadBotPlugin
    {
        public string PluginID => "Data Enrichment Helper";

        public void Init()
        {
            var manager = new EnrichmentManager();
            Events.CallbackEvent += manager.HandleCallback;
            Events.TextEvent += manager.HandleText;
        }

        public void PostInit()
        {
        }
    }
}