using System;
using System.Collections.Generic;

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
            Options,
            OptionsExpandable,
            OptionsMulti,
            Done
        }

        public readonly int Id = 1;
        private readonly long _chatId;
        private string _name;
        private int? _batchCount;
        private readonly PartialSource _dataSource;
        private bool? _isRandomOrder;
        private readonly PartialOutput _dataOutput;
        private List<string> _options;
        private bool? _canAddOptions;
        private bool? _canSelectMultipleOptions;

        public PartialSession(long chatId)
        {
            _chatId = chatId;
            _dataSource = new PartialSource();
            _dataOutput = new PartialOutput();
        }

        public PartialSession(PartialData data)
        {
            Id = data.Id;
            _chatId = data.ChatId;
            _name = data.Name;
            _batchCount = data.BatchCount;
            _dataSource = new PartialSource(data.DataSource);
            _isRandomOrder = data.IsRandomOrder;
            _dataOutput = new PartialOutput(data.DataOutput);
            _options = data.Options;
            _canAddOptions = data.CanAddOptions;
            _canSelectMultipleOptions = data.CanSelectMultipleOptions;
        }

        public SessionParts NextPart()
        {
            if (_name == null) return SessionParts.Name;

            if (_batchCount == null) return SessionParts.BatchCount;

            if (_dataSource.NextPart() != PartialSource.SourceParts.Done)
            {
                return SessionParts.DataSource;
            }

            if (_isRandomOrder == null) return SessionParts.RandomOrder;

            if (_dataOutput.NextPart() != PartialOutput.OutputParts.Done)
            {
                return SessionParts.DataOutput;
            }
            if (!_dataOutput.AllowMultipleOptions()) _canSelectMultipleOptions = false;

            if (_options == null) return SessionParts.Options;
            if (_canAddOptions == null) return SessionParts.OptionsExpandable;
            if (_canSelectMultipleOptions == null) return SessionParts.OptionsMulti;

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
                case SessionParts.Options:
                    return new CreateSessionOptions();
                case SessionParts.OptionsExpandable:
                    return new CreateSessionOptionsExpandable();
                case SessionParts.OptionsMulti:
                    return new CreateSessionOptionsMulti();
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
                case SessionParts.Options:
                    return true;
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
                case SessionParts.Options:
                    _options = new List<string>(text.Split(','));
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
                case SessionParts.OptionsExpandable:
                    return true;
                case SessionParts.OptionsMulti:
                    return true;
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

            if (
                nextPart == SessionParts.OptionsExpandable
                && callbackData.StartsWith($"{CreateSessionOptionsExpandable.CallbackName}:")
            )
            {
                var expandable = callbackData.Split(':')[1];
                _canAddOptions = bool.Parse(expandable);
            }

            if (
                nextPart == SessionParts.OptionsMulti
                && callbackData.StartsWith($"{CreateSessionOptionsMulti.CallbackName}:")
            )
            {
                var multi = callbackData.Split(':')[1];
                _canSelectMultipleOptions = bool.Parse(multi);
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
                || _options == null
                || _canAddOptions == null
                || _canSelectMultipleOptions == null
            )
            {
                throw new ArgumentNullException();
            }

            var dataSource = _dataSource.BuildSource();
            var dataOutput = _dataOutput.BuildOutput(_name, dataSource.ToData());

            return new EnrichmentSession(
                nextId,
                _chatId,
                _name,
                (int) _batchCount,
                dataSource,
                (bool) _isRandomOrder,
                dataOutput,
                _options,
                (bool) _canAddOptions,
                (bool) _canSelectMultipleOptions
            );
        }

        public PartialData ToData()
        {
            return new PartialData
            {
                Id = Id,
                ChatId = _chatId,
                Name = _name,
                BatchCount = _batchCount,
                DataSource = _dataSource.ToData(),
                IsRandomOrder = _isRandomOrder,
                DataOutput = _dataOutput.ToData(),
                Options = _options,
                CanAddOptions = _canAddOptions,
                CanSelectMultipleOptions = _canSelectMultipleOptions
            };
        }

        public class PartialData
        {
            public int Id { get; set; }
            public long ChatId { get; set; }
            public string Name { get; set; }
            public int? BatchCount { get; set; }
            public PartialSource.PartialData DataSource { get; set; }
            public bool? IsRandomOrder { get; set; }
            public PartialOutput.PartialData DataOutput { get; set; }
            public List<string> Options { get; set; }
            public bool? CanAddOptions { get; set; }
            public bool? CanSelectMultipleOptions { get; set; }
        }
    }
}