using System;

namespace TelegramDataEnrichment.Sessions
{
    public class EnrichmentSession
    {
        public bool IsActive { get; private set; }
        public string Name { get; }
        public int Id { get; }
        private readonly int _batchCount;
        private readonly DataSource _dataSource;
        private readonly bool _isRandomOrder;
        private readonly DataOutput _dataOutput;

        public EnrichmentSession(
            int id, 
            string name,
            int batchCount,
            DataSource dataSource, 
            bool isRandomOrder,
            DataOutput dataOutput
            )
        {
            Id = id;
            Name = name; // User friendly name
            IsActive = false;
            _batchCount = batchCount;  // How many to post at once
            _dataSource = dataSource;
            _isRandomOrder = isRandomOrder;
            _dataOutput = dataOutput;
        }

        public EnrichmentSession(SessionData data)
        {
            if (data.DataSource == null || data.DataOutput == null)
            {
                throw new ArgumentNullException();
            }
            Id = data.Id;
            Name = data.Name;
            IsActive = data.IsActive;
            _batchCount = data.BatchCount;
            _dataSource = DataSource.FromData(data.DataSource);
            _isRandomOrder = data.IsRandomOrder;
            _dataOutput = DataOutput.FromData(data.DataOutput, data.DataSource);
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
                DataSource = _dataSource.ToData(),
                IsRandomOrder = _isRandomOrder,
                DataOutput = _dataOutput.ToData()
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
            public bool IsRandomOrder { get; set; }
            public DataOutput.DataOutputData DataOutput { get; set; }
            // public List<string> Options { get; set; }
            // public bool IsMultiOption { get; set; }
            // public bool CanManuallyInput { get; set; }
        }
    }
}