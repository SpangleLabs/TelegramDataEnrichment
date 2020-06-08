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
            RandomOrder,
            DataOutput,
            Done
        }

        public readonly int Id = 1;
        private string _name;
        private int? _batchCount;
        private readonly PartialSource _dataSource;
        private bool? _isRandomOrder;
        private readonly PartialOutput _dataOutput;

        public PartialSession()
        {
            _dataSource = new PartialSource();
            _dataOutput = new PartialOutput();
        }

        public PartialSession(PartialData data)
        {
            Id = data.Id;
            _name = data.Name;
            _batchCount = data.BatchCount;
            _dataSource = new PartialSource(data.DataSource);
            _isRandomOrder = data.IsRandomOrder;
            _dataOutput = new PartialOutput(data.DataOutput);
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

            if (_isRandomOrder == null)
            {
                return SessionParts.RandomOrder;
            }
            
            if (_dataOutput.NextPart() != PartialOutput.OutputParts.Done)
            {
                return SessionParts.DataOutput;
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
                case SessionParts.RandomOrder:
                    return new CreateSessionRandomOrder();
                case SessionParts.DataOutput:
                    return _dataOutput.NextMenu(_dataSource);
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
                case SessionParts.DataOutput:
                    return _dataOutput.WaitingForText();
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
                case SessionParts.DataOutput:
                    _dataOutput.AddText(text);
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
                case SessionParts.RandomOrder:
                    return true;
                case SessionParts.DataOutput:
                    return _dataOutput.WaitingForCallback();
                default:
                    return false;
            }
        }

        public void AddCallback(string callbackData)
        {
            var nextPart = NextPart();
            if (
                nextPart == SessionParts.BatchCount
                && callbackData.StartsWith($"{CreateSessionBatchSizeMenu.CallbackName}:")
            )
            {
                var batchSize = callbackData.Split(':')[1];
                _batchCount = int.Parse(batchSize);
            }

            if (nextPart == SessionParts.DataSource)
            {
                _dataSource.AddCallback(callbackData);
            }

            if (
                nextPart == SessionParts.RandomOrder 
                && callbackData.StartsWith($"{CreateSessionRandomOrder.CallbackName}:")
                )
            {
                var randomOrder = callbackData.Split(':')[1];
                _isRandomOrder = bool.Parse(randomOrder);
            }

            if (nextPart == SessionParts.DataOutput)
            {
                _dataOutput.AddCallback(callbackData);
            }
        }

        public EnrichmentSession BuildSession(int nextId)
        {
            if (
                _name == null 
                || _batchCount == null 
                || _dataSource.NextPart() != PartialSource.SourceParts.Done
                || _isRandomOrder == null
                || _dataOutput.NextPart() != PartialOutput.OutputParts.Done
                )
            {
                throw new ArgumentNullException();
            }
            
            var dataSource = _dataSource.BuildSource();
            var dataOutput = _dataOutput.BuildOutput(dataSource.ToData());

            return new EnrichmentSession(nextId, _name, (int) _batchCount, dataSource, (bool) _isRandomOrder, dataOutput);
        }

        public PartialData ToData()
        {
            return new PartialData
            {
                Id = Id,
                Name = _name,
                BatchCount = _batchCount,
                DataSource = _dataSource.ToData(),
                IsRandomOrder = _isRandomOrder,
                DataOutput = _dataOutput.ToData()
            };
        }

        public class PartialData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int? BatchCount { get; set; }
            public PartialSource.PartialData DataSource { get; set; }
            public bool? IsRandomOrder { get; set; }
            public PartialOutput.PartialData DataOutput { get; set; }
        }
    }
}