﻿using System;
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
        private readonly DatumIdIndex _datumIdIndex;

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
            _datumIdIndex = new DatumIdIndex();
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
            
            var callbackIdToMessageId = data.CallbackIdToMessageId ?? new Dictionary<int, long>();
            var callbackIdToDatumId = data.CallbackIdToDatumId ?? new Dictionary<int, string>();
            _datumIdIndex = new DatumIdIndex(callbackIdToMessageId, callbackIdToDatumId);
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
            var datumId = _datumIdIndex.GetDatumIdFromCallbackId(callbackId);
            var matchingData = IncompleteData().Where(d => d.DatumId.Equals(datumId)).ToList();
            
            
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
            var callbackId = _datumIdIndex.GetCallbackIdFromDatumId(datum.DatumId);
            var messageId = _datumIdIndex.GetMessageIdFromCallbackId(callbackId);
            Methods.deleteMessage(_chatId, messageId);
            _datumIdIndex.RemoveMessageByCallbackId(callbackId);
        }

        private void PostMessages()
        {
            if (_datumIdIndex.MessageCount() >= _batchCount) return;
            var incompleteData = IncompleteData();
            if (_isRandomOrder)
            {
                incompleteData = incompleteData.OrderBy(a => Guid.NewGuid()).ToList();
            }

            var postData = incompleteData.Take(_batchCount - _datumIdIndex.MessageCount()).ToList();
            if (postData.Count == 0)
            {
                Methods.sendMessage(_chatId, "Enrichment session complete!");
                Stop();
                return;
            }
            foreach (var datum in postData)
            {
                var callbackId = _datumIdIndex.GetCallbackIdFromDatumId(datum.DatumId);
                var keyboard = Keyboard(callbackId);
                var result = datum.Post(_chatId, keyboard);
                _datumIdIndex.AddMessageId(result.result.message_id, callbackId);
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
            foreach (var messageId in _datumIdIndex.MessageIds())
            {
                Methods.deleteMessage(_chatId, messageId);
            }
            _datumIdIndex.ClearMessages();
        }

        public List<Datum> AllData()
        {
            var sourceData = _dataSource.ListData();
            var completeData = _dataOutput.ListCompleted();
            var notInSourceData = completeData.Where(d => sourceData.All(d2 => d2.DatumId != d.DatumId));
            var allData = sourceData.Concat(notInSourceData).ToList();
            allData.ForEach(d => _datumIdIndex.AddDatumId(d.DatumId));
            return allData;
        }

        public List<Datum> IncompleteData()
        {
            var incompleteData = _dataOutput.RemoveCompleted(_dataSource.ListData());
            incompleteData.ForEach(d => _datumIdIndex.AddDatumId(d.DatumId));
            return incompleteData;
        }

        public List<Datum> CompletedData()
        {
            var completeData = _dataOutput.ListCompleted();
            completeData.ForEach(d => _datumIdIndex.AddDatumId(d.DatumId));
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
                CallbackIdToMessageId = _datumIdIndex.CallbackIdToMessageId,
                CallbackIdToDatumId = _datumIdIndex.CallbackIdToDatumId
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
            public Dictionary<int, long> CallbackIdToMessageId { get; set; }
            public Dictionary<int, string> CallbackIdToDatumId { get; set; }
        }

        private class DatumIdIndex
        {
            public Dictionary<int, long> CallbackIdToMessageId { get; }
            public Dictionary<int, string> CallbackIdToDatumId { get; }
            private readonly Dictionary<string, int> _datumIdToCallbackId;
            private int _nextCallbackId;

            public DatumIdIndex()
            {
                _nextCallbackId = 0;
            }

            public DatumIdIndex(Dictionary<int, long> callbackIdToMessageId, Dictionary<int, string> callbackIdToDatumId)
            {
                CallbackIdToMessageId = callbackIdToMessageId;
                CallbackIdToDatumId = callbackIdToDatumId;
                _nextCallbackId = callbackIdToDatumId.Keys.Max();
                _datumIdToCallbackId = callbackIdToDatumId.ToDictionary((i) => i.Value, (i) => i.Key);
            }

            public void AddDatumId(string datumId)
            {
                if (_datumIdToCallbackId.ContainsKey(datumId) || CallbackIdToDatumId.ContainsValue(datumId)) return;
                var callbackId = _nextCallbackId++;
                CallbackIdToDatumId.Add(callbackId, datumId);
                _datumIdToCallbackId.Add(datumId, callbackId);
            }

            public void AddMessageId(long messageId, int callbackId)
            {
                CallbackIdToMessageId.Add(callbackId, messageId);
            }

            public void RemoveMessageByCallbackId(int callbackId)
            {
                CallbackIdToMessageId.Remove(callbackId);
            }

            public string GetDatumIdFromCallbackId(int callbackId)
            {
                return CallbackIdToDatumId[callbackId];
            }

            public int GetCallbackIdFromDatumId(string datumId)
            {
                return _datumIdToCallbackId[datumId];
            }

            public long GetMessageIdFromCallbackId(int callbackId)
            {
                return CallbackIdToMessageId[callbackId];
            }

            public int MessageCount()
            {
                return CallbackIdToMessageId.Count;
            }

            public List<long> MessageIds()
            {
                return CallbackIdToMessageId.Values.ToList();
            }

            public void ClearMessages()
            {
                CallbackIdToMessageId.Clear();
            }
        }
    }
}