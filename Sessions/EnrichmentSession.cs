using System;
using System.Collections.Generic;
using System.Linq;
using DreadBot;

namespace TelegramDataEnrichment.Sessions
{
    public class EnrichmentSession
    {
        public bool IsActive { get; private set; }
        public string Name { get; }
        public int Id { get; }
        private readonly long _chatId;
        private readonly int _batchCount;
        private readonly DataSource _dataSource;
        private readonly bool _isRandomOrder;
        private readonly DataOutput _dataOutput;
        private readonly List<string> _options;
        private readonly bool _canAddOptions;
        private readonly bool _canSelectMultipleOptions;
        private readonly List<long> _messages;

        public EnrichmentSession(
            int id,
            long chatId,
            string name,
            int batchCount,
            DataSource dataSource,
            bool isRandomOrder,
            DataOutput dataOutput,
            List<string> options,
            bool canAddOptions,
            bool canSelectMultipleOptions
        )
        {
            Id = id;
            _chatId = chatId;
            Name = name; // User friendly name
            IsActive = false;
            _batchCount = batchCount; // How many to post at once
            _dataSource = dataSource;
            _isRandomOrder = isRandomOrder;
            _dataOutput = dataOutput;
            _options = options;
            _canAddOptions = canAddOptions;
            _canSelectMultipleOptions = canSelectMultipleOptions;
            _messages = new List<long>();
        }

        public EnrichmentSession(SessionData data)
        {
            if (data.DataSource == null || data.DataOutput == null)
            {
                throw new ArgumentNullException();
            }

            Id = data.Id;
            _chatId = data.ChatId;
            Name = data.Name;
            IsActive = data.IsActive;
            _batchCount = data.BatchCount;
            _dataSource = DataSource.FromData(data.DataSource);
            _isRandomOrder = data.IsRandomOrder;
            _dataOutput = DataOutput.FromData(data.DataOutput, data.DataSource);
            _options = data.Options;
            _canAddOptions = data.CanAddOptions;
            _canSelectMultipleOptions = data.CanSelectMultipleOptions;
            _messages = data.MessageIds ?? new List<long>();
        }

        public void Start()
        {
            IsActive = true;
            if (_messages.Count < _batchCount)
            {
                var data = _dataSource.ListData();
                var incompleteData = _dataOutput.RemoveCompleted(data);
                if (_isRandomOrder)
                {
                    incompleteData = incompleteData.OrderBy(a => Guid.NewGuid()).ToList();
                }

                var postData = incompleteData.Take(_batchCount - _messages.Count);
                foreach (var datum in postData)
                {
                    var keyboard = new InlineKeyboardMarkup();
                    keyboard.addCallbackButton("Errr", "erm", 0);
                    datum.Post(_chatId, keyboard);
                }
            }
        }

        public void Stop()
        {
            IsActive = false;
        }

        public List<Datum> AllData()
        {
            var sourceData = _dataSource.ListData();
            var completeData = _dataOutput.ListCompleted();
            var notInSourceData = completeData.Where(d => sourceData.All(d2 => d2.DatumId != d.DatumId));
            var allData = sourceData.Concat(notInSourceData).ToList();
            return allData;
        }

        public List<Datum> IncompleteData()
        {
            return _dataOutput.RemoveCompleted(_dataSource.ListData());
        }

        public List<Datum> CompletedData()
        {
            return _dataOutput.ListCompleted();
        }

        public SessionData ToData()
        {
            return new SessionData
            {
                Id = Id,
                ChatId = _chatId,
                Name = Name,
                IsActive = IsActive,
                BatchCount = _batchCount,
                DataSource = _dataSource.ToData(),
                IsRandomOrder = _isRandomOrder,
                DataOutput = _dataOutput.ToData(),
                Options = _options,
                CanAddOptions = _canAddOptions,
                CanSelectMultipleOptions = _canSelectMultipleOptions,
                MessageIds = _messages
            };
        }

        public class SessionData
        {
            public int Id { get; set; }
            public long ChatId { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public List<long> MessageIds { get; set; }
            public int BatchCount { get; set; }
            public DataSource.DataSourceData DataSource { get; set; }
            public bool IsRandomOrder { get; set; }
            public DataOutput.DataOutputData DataOutput { get; set; }
            public List<string> Options { get; set; }
            public bool CanAddOptions { get; set; }
            public bool CanSelectMultipleOptions { get; set; }
        }
    }
}