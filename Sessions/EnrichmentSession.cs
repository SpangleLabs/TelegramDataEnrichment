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

        public EnrichmentSession(int id, string name, int batchCount, DataSource dataSource, bool isRandomOrder)
        {
            Id = id;
            Name = name; // User friendly name
            IsActive = false;
            _batchCount = batchCount;  // How many to post at once
            _dataSource = dataSource;
            _isRandomOrder = isRandomOrder;
        }

        public EnrichmentSession(SessionData data)
        {
            Id = data.Id;
            Name = data.Name;
            IsActive = data.IsActive;
            _batchCount = data.BatchCount;
            _dataSource = DataSource.FromData(data.DataSource);
            _isRandomOrder = data.IsRandomOrder;
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
                IsRandomOrder = _isRandomOrder
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
            // public List<string> Options { get; set; }
            // public bool IsMultiOption { get; set; }
            // public bool CanManuallyInput { get; set; }
            // public DataOutputData DataOutput { get; set; }
        }
    }
}