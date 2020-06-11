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
        private const string CallbackPrev = "prev";
        private const string CallbackNext = "next";
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
        private readonly SessionIdIndex _idIndex;

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
            _idIndex = new SessionIdIndex(_options);
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
            _dataOutput = DataOutput.FromData(data.Name, data.DataOutput, data.DataSource);
            _options = data.Options;
            _canAddOptions = data.CanAddOptions;
            _canSelectMultipleOptions = data.CanSelectMultipleOptions;
            _idIndex = new SessionIdIndex(data.IdIndex);
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
            
            var callbackId = int.Parse(split[2]);
            var datumId = _idIndex.GetDatumIdFromCallbackId(callbackId);
            var optionId = split[3];

            if (optionId.Equals(CallbackPrev))
            {
                _idIndex.PrevPageByCallbackId(callbackId);
                UpdateKeyboard(callbackId);
                return;
            }

            if (optionId.Equals(CallbackNext))
            {
                _idIndex.NextPageByCallbackId(callbackId);
                UpdateKeyboard(callbackId);
                return;
            }
            
            if (optionId.Equals(CallbackDone))
            {
                MarkDatumDone(datumId);
                return;
            }

            var option = _idIndex.GetOptionByOptionId(int.Parse(optionId));
            MarkDatum(datumId, option);
        }

        private void MarkDatum(DatumId datumId, string option)
        {
            var matchingData = IncompleteData().Where(d => d.DatumId.Equals(datumId)).ToList();
            foreach (var datum in matchingData)
            {
                _dataOutput.HandleDatum(datum, option);
                if (!_canSelectMultipleOptions) RemoveMessage(datum);
            }
            PostMessages();
        }

        private void MarkDatumDone(DatumId datumId)
        {
            var matchingData = IncompleteData().Where(d => d.DatumId.Equals(datumId)).ToList();
            foreach (var datum in matchingData)
            {
                _dataOutput.HandleDatumDone(datum);
                RemoveMessage(datum);
            }
            PostMessages();
        }

        public Menu HandleMessage(Message msg)
        {
            if (!_canAddOptions) return null;
            var replyingTo = msg?.reply_to_message?.message_id;
            if (replyingTo == null) return null;
            if (!_idIndex.MessageIds().Contains((long) replyingTo)) return null;
            var newOption = msg.text.Trim();
            _options.Add(newOption);
            _idIndex.AddOption(newOption);

            var callbackId = _idIndex.GetCallbackIdFromMessageId((long) replyingTo);
            var datumId = _idIndex.GetDatumIdFromCallbackId(callbackId);
            MarkDatum(datumId, newOption);
            return new AddedNewSessionOption(this, newOption);
        }

        private void RemoveMessage(Datum datum)
        {
            var callbackId = _idIndex.GetCallbackIdFromDatumId(datum.DatumId);
            var messageId = _idIndex.GetMessageIdFromCallbackId(callbackId);
            Methods.deleteMessage(_chatId, messageId);
            _idIndex.RemoveMessageByCallbackId(callbackId);
        }

        private void PostMessages()
        {
            if (_idIndex.MessageCount() >= _batchCount) return;
            var incompleteData = IncompleteData();
            if (_isRandomOrder)
            {
                incompleteData = incompleteData.OrderBy(a => Guid.NewGuid()).ToList();
            }

            var postData = incompleteData.Take(_batchCount - _idIndex.MessageCount()).ToList();
            if (postData.Count == 0)
            {
                Methods.sendMessage(_chatId, "Enrichment session complete!");
                Stop();
                return;
            }
            foreach (var datum in postData)
            {
                var callbackId = _idIndex.GetCallbackIdFromDatumId(datum.DatumId);
                var keyboard = Keyboard(callbackId);
                var result = datum.Post(_chatId, keyboard);
                _idIndex.AddMessageId(result.result.message_id, callbackId);
            }
        }

        private InlineKeyboardMarkup Keyboard(int callbackId)
        {
            const int maxCols = 3;
            const int maxRows = 5;
            var perPage = maxCols * maxRows;
            
            var keyboard = new InlineKeyboardMarkup();
            var currentPage = _idIndex.GetPageFromCallbackId(callbackId);
            var totalOptions = _options.Count;
            var pages = ((totalOptions - 1) / perPage) + 1;
            var optionsOnPage = _options.Skip(perPage * currentPage).Take(perPage).ToList();
            var numOptionsOnPage = optionsOnPage.Count;
            var columns = ((numOptionsOnPage - 1) / maxRows) + 1;

            var datumId = _idIndex.GetDatumIdFromCallbackId(callbackId);
            var selectedOptions = AllData().Where(d => d.DatumId.Equals(datumId)).SelectMany(d => _dataOutput.GetOptionsForData(d)).ToList();

            var buttonId = 0;
            var rowId = 0;
            foreach (var option in optionsOnPage)
            {
                var optionId = _idIndex.GetOptionIdByOption(option);
                var selected = selectedOptions.Contains(option) ? "✔ " : "";
                var text = $"{selected}{option}";
                keyboard.addCallbackButton(text, $"{CallbackName}:{Id}:{callbackId}:{optionId}", rowId);
                buttonId++;
                if (buttonId % columns == 0)
                {
                    rowId++;
                }
            }

            if (pages > 1 && currentPage > 0)
            { 
                keyboard.addCallbackButton("Prev page", $"{CallbackName}:{Id}:{callbackId}:{CallbackPrev}", rowId);
            }
            if (_canSelectMultipleOptions)
            {
                keyboard.addCallbackButton("*Done*", $"{CallbackName}:{Id}:{callbackId}:{CallbackDone}", rowId);
            }
            if (pages > 1 && currentPage + 1 < pages) { 
                keyboard.addCallbackButton("Next page", $"{CallbackName}:{Id}:{callbackId}:{CallbackNext}", rowId);
            }

            rowId++;
            
            keyboard.addCallbackButton("End session", $"{StopSessionMenu.CallbackName}:{Id}", rowId);

            return keyboard;
        }

        private void UpdateKeyboard(int callbackId)
        {
            var messageId = _idIndex.GetMessageIdFromCallbackId(callbackId);
            Methods.editMessageReplyMarkup(_chatId, messageId, keyboard: Keyboard(callbackId));
        }

        public void Stop()
        {
            IsActive = false;
            foreach (var messageId in _idIndex.MessageIds())
            {
                Methods.deleteMessage(_chatId, messageId);
            }
            _idIndex.ClearMessages();
        }

        public List<Datum> AllData()
        {
            var sourceData = _dataSource.ListData();
            var completeData = _dataOutput.ListCompleted();
            var notInSourceData = completeData.Where(d => sourceData.All(d2 => !Equals(d2, d)));
            var allData = sourceData.Concat(notInSourceData).ToList();
            allData.ForEach(d => _idIndex.AddDatumId(d.DatumId));
            return allData;
        }

        public List<Datum> IncompleteData()
        {
            var incompleteData = _dataOutput.RemoveCompleted(_dataSource.ListData());
            incompleteData.ForEach(d => _idIndex.AddDatumId(d.DatumId));
            return incompleteData;
        }

        public List<Datum> CompletedData()
        {
            var completeData = _dataOutput.ListCompleted();
            completeData.ForEach(d => _idIndex.AddDatumId(d.DatumId));
            return completeData;
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
                IdIndex = _idIndex.ToData()
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
            public SessionIdIndex.IdIndexData IdIndex { get; set; }
        }
    }
}