using System;

namespace TelegramDataEnrichment.Sessions
{
    public class PartialSession
    {
        public enum SessionParts
        {
            Name,
            BatchCount,
            DataSource,
            Done
        }

        public readonly int Id = 1;
        private string _name;
        private int? _batchCount;
        private readonly PartialSource _dataSource;

        public PartialSession()
        {
            _dataSource = new PartialSource();
        }

        public PartialSession(PartialData data)
        {
            Id = data.Id;
            _name = data.Name;
            _batchCount = data.BatchCount;
            _dataSource = new PartialSource(data.DataSource);
        }

        public SessionParts NextPart()
        {
            if (_name == null)
            {
                return SessionParts.Name;
            }

            if (_batchCount == null)
            {
                return SessionParts.BatchCount;
            }

            if (_dataSource.NextPart() != PartialSource.SourceParts.Done)
            {
                return SessionParts.DataSource;
            }

            return SessionParts.Done;
        }

        public Menu NextMenu()
        {
            switch (NextPart())
            {
                case SessionParts.Name:
                    return new CreateSessionMenu();
                case SessionParts.BatchCount:
                    return new CreateSessionBatchSizeMenu();
                case SessionParts.DataSource:
                    return _dataSource.NextMenu();
                case SessionParts.Done:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool WaitingForText()
        {
            switch (NextPart())
            {
                case SessionParts.Name:
                    return true;
                case SessionParts.DataSource:
                    return _dataSource.WaitingForText();
                default:
                    return false;
            }
        }

        public void AddText(string text)
        {
            var nextPart = NextPart();
            switch (nextPart)
            {
                case SessionParts.Name:
                    _name = text;
                    break;
                case SessionParts.DataSource:
                    _dataSource.AddText(text);
                    break;
            }
        }

        public bool WaitingForCallback()
        {
            switch (NextPart())
            {
                case SessionParts.BatchCount:
                    return true;
                case SessionParts.DataSource:
                    return _dataSource.WaitingForCallback();
                default:
                    return false;
            }
        }

        public void AddCallback(string callbackData)
        {
            if (
                NextPart() == SessionParts.BatchCount
                && callbackData.StartsWith($"{CreateSessionBatchSizeMenu.CallbackName}:")
            )
            {
                var batchSize = callbackData.Split(':')[1];
                _batchCount = int.Parse(batchSize);
            }

            if (NextPart() == SessionParts.DataSource)
            {
                _dataSource.AddCallback(callbackData);
            }
        }

        public EnrichmentSession BuildSession(int nextId)
        {
            if (_name == null || _batchCount == null || _dataSource.NextPart() != PartialSource.SourceParts.Done)
            {
                throw new ArgumentNullException();
            }
            
            var dataSource = _dataSource.BuildSource();

            return new EnrichmentSession(nextId, _name, (int) _batchCount, dataSource);
        }

        public PartialData ToData()
        {
            return new PartialData
            {
                Id = Id,
                Name = _name,
                BatchCount = _batchCount,
                DataSource = _dataSource.ToData()
            };
        }

        public class PartialData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int? BatchCount { get; set; }
            public PartialSource.PartialData DataSource { get; set; }
        }
    }
}