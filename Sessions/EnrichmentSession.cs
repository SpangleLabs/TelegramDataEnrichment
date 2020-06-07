﻿namespace TelegramDataEnrichment.Sessions
{
    public class EnrichmentSession
    {
        public bool IsActive { get; private set; }
        public string Name { get; }
        public int Id { get; }
        private readonly int _batchCount;
        private readonly DataSource _dataSource;

        public EnrichmentSession(int id, string name, int batchCount, DataSource dataSource)
        {
            Id = id;
            Name = name; // User friendly name
            IsActive = false;
            _batchCount = batchCount;  // How many to post at once
            _dataSource = dataSource;
        }

        public EnrichmentSession(SessionData data)
        {
            Id = data.Id;
            Name = data.Name;
            IsActive = data.IsActive;
            _batchCount = data.BatchCount;
            _dataSource = DataSource.FromData(data.DataSource);
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
                IsActive = IsActive,
                BatchCount = _batchCount,
                DataSource = _dataSource.ToData()
            };
        }

        public class SessionData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            // public List<long> messageIds { get; set; }
            public int BatchCount { get; set; }
            public DataSource.DataSourceData DataSource { get; set; }
            // public bool isRandomOrder { get; set; }
            // public List<string> options { get; set; }
            // public bool isMultiOption { get; set; }
            // public bool canManuallyInput { get; set; }
            // public DataOutputData dataOutput { get; set; }
        }
    }
}