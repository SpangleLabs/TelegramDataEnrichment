using System;
using System.Collections.Generic;
using System.Linq;
using DreadBot;

namespace TelegramDataEnrichment.Sessions
{
    public class EnrichmentSession
    {
        public const string CallbackName = "enrich";
        private const string CallbackDone = "done";
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
        private readonly Dictionary<long, long> _dataToMessages;

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
            _dataToMessages = new Dictionary<long, long>();
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
            _dataToMessages = data.DataToMessages ?? new Dictionary<long, long>();
        }

        public void Start()
        {
            IsActive = true;
            PostMessages();
        }

        public void HandleCallback(string callbackData)
        {
            var split = callbackData.Split(':');
            var sessionId = split[1];
            if (!Id.ToString().Equals(sessionId)) return;
            
            var datumIdNumber = split[2];
            var matchingData = IncompleteData().Where(d => d.IdNumber.ToString().Equals(datumIdNumber)).ToList();
            
            
            var optionId = split[3];
            if (optionId.Equals(CallbackDone))
            {
                foreach (var datum in matchingData)
                {
                    _dataOutput.HandleDatumDone(datum);
                    RemoveMessage(datum);
                }
                PostMessages();
                return;
            }

            var option = _options[int.Parse(optionId)];
            foreach (var datum in matchingData)
            {
                _dataOutput.HandleDatum(datum, option);
                if (!_canSelectMultipleOptions) RemoveMessage(datum);
            }
            PostMessages();
        }

        private void RemoveMessage(Datum datum)
        {
            var messageId = _dataToMessages[datum.IdNumber];
            Methods.deleteMessage(_chatId, messageId);
            _dataToMessages.Remove(datum.IdNumber);
        }

        private void PostMessages()
        {
            if (_dataToMessages.Count >= _batchCount) return;
            var incompleteData = IncompleteData();
            if (_isRandomOrder)
            {
                incompleteData = incompleteData.OrderBy(a => Guid.NewGuid()).ToList();
            }

            var postData = incompleteData.Take(_batchCount - _dataToMessages.Count).ToList();
            if (postData.Count == 0)
            {
                Methods.sendMessage(_chatId, "Enrichment session complete!");
                Stop();
                return;
            }
            foreach (var datum in postData)
            {
                var keyboard = Keyboard(datum.IdNumber);
                var result = datum.Post(_chatId, keyboard);
                _dataToMessages.Add(datum.IdNumber, result.result.message_id);
            }
        }

        private InlineKeyboardMarkup Keyboard(int datumId)
        {
            var keyboard = new InlineKeyboardMarkup();
            var optionId = 0;
            foreach (var option in _options)
            {
                keyboard.addCallbackButton(option, $"{CallbackName}:{Id}:{datumId}:{optionId}", optionId);
                optionId++;
            }

            if (_canSelectMultipleOptions)
            {
                keyboard.addCallbackButton("*Done*", $"{CallbackName}:{Id}:{datumId}:{CallbackDone}", optionId);
            }

            return keyboard;
        }

        public void Stop()
        {
            IsActive = false;
            foreach (var pair in _dataToMessages)
            {
                Methods.deleteMessage(_chatId, pair.Value);
            }
            _dataToMessages.Clear();
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
                DataToMessages = _dataToMessages
            };
        }

        public class SessionData
        {
            public int Id { get; set; }
            public long ChatId { get; set; }
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public int BatchCount { get; set; }
            public DataSource.DataSourceData DataSource { get; set; }
            public bool IsRandomOrder { get; set; }
            public DataOutput.DataOutputData DataOutput { get; set; }
            public List<string> Options { get; set; }
            public bool CanAddOptions { get; set; }
            public bool CanSelectMultipleOptions { get; set; }
            public Dictionary<long, long> DataToMessages { get; set; }
        }
    }
}