using System;
using System.Collections.Generic;
using System.Linq;

namespace TelegramDataEnrichment.Sessions
{
    public class SessionIdIndex
    {
        private readonly Dictionary<int, long> _callbackIdToMessageId;
        private readonly Dictionary<long, int> _messageIdToCallbackId;
        private readonly Dictionary<int, string> _callbackIdToDatumId;
        private readonly Dictionary<string, int> _datumIdToCallbackId;
        private readonly Dictionary<long, int> _messageIdToPage;
        private readonly Dictionary<string, int> _optionToOptionId;
        private readonly Dictionary<int, string> _optionIdToOption;
        private int _nextCallbackId;
        private int _nextOptionId;

        public SessionIdIndex(List<string> options)
        {
            _callbackIdToMessageId = new Dictionary<int, long>();
            _messageIdToCallbackId = new Dictionary<long, int>();

            _callbackIdToDatumId = new Dictionary<int, string>();
            _datumIdToCallbackId = new Dictionary<string, int>();

            _messageIdToPage = new Dictionary<long, int>();
            _nextCallbackId = 0;

            _nextOptionId = 0;
            _optionToOptionId = options.ToDictionary(o => o, o => _nextOptionId++);
            _optionIdToOption = _optionToOptionId.ToDictionary((i) => i.Value, (i) => i.Key);
        }

        public SessionIdIndex(
            IdIndexData data
        )
        {
            if (
                data?.CallbackIdToMessageId == null
                || data.CallbackIdToDatumId == null
                || data.MessageIdToPage == null
                || data.OptionToOptionId == null
            )
            {
                throw new ArgumentNullException();
            }

            _callbackIdToMessageId = data.CallbackIdToMessageId;
            _callbackIdToDatumId = data.CallbackIdToDatumId;
            _messageIdToPage = data.MessageIdToPage;
            _optionToOptionId = data.OptionToOptionId;

            _nextCallbackId = _callbackIdToDatumId.Count == 0 ? 0 : _callbackIdToDatumId.Keys.Max() + 1;
            _nextOptionId = _optionToOptionId.Count == 0 ? 0 : _optionToOptionId.Values.Max() + 1;
            _messageIdToCallbackId = _callbackIdToMessageId.ToDictionary((i) => i.Value, (i) => i.Key);
            _datumIdToCallbackId = _callbackIdToDatumId.ToDictionary((i) => i.Value, (i) => i.Key);
            _optionIdToOption = _optionToOptionId.ToDictionary((i) => i.Value, (i) => i.Key);
        }

        public void AddDatumId(DatumId datumId)
        {
            var datumStr = datumId.ToString();
            if (_datumIdToCallbackId.ContainsKey(datumStr) || _callbackIdToDatumId.ContainsValue(datumStr)) return;
            var callbackId = _nextCallbackId++;
            _callbackIdToDatumId.Add(callbackId, datumStr);
            _datumIdToCallbackId.Add(datumStr, callbackId);
        }

        public void AddMessageId(long messageId, int callbackId)
        {
            _callbackIdToMessageId.Add(callbackId, messageId);
            _messageIdToCallbackId.Add(messageId, callbackId);
            _messageIdToPage.Add(messageId, 0);
        }

        public void AddOption(string option)
        {
            if (_optionToOptionId.ContainsKey(option) || _optionIdToOption.ContainsValue(option)) return;
            var optionId = _nextOptionId++;
            _optionToOptionId.Add(option, optionId);
            _optionIdToOption.Add(optionId, option);
        }

        public void RemoveMessageByCallbackId(int callbackId)
        {
            var messageId = _callbackIdToMessageId[callbackId];
            _callbackIdToMessageId.Remove(callbackId);
            _messageIdToCallbackId.Remove(messageId);
            _messageIdToPage.Remove(messageId);
        }

        public DatumId GetDatumIdFromCallbackId(int callbackId)
        {
            return new DatumId(_callbackIdToDatumId[callbackId]);
        }

        public int GetCallbackIdFromDatumId(DatumId datumId)
        {
            return _datumIdToCallbackId[datumId.ToString()];
        }

        public long GetMessageIdFromCallbackId(int callbackId)
        {
            return _callbackIdToMessageId[callbackId];
        }

        public int GetCallbackIdFromMessageId(long messageId)
        {
            return _messageIdToCallbackId[messageId];
        }

        public int GetPageFromCallbackId(int callbackId)
        {
            if (!_callbackIdToMessageId.ContainsKey(callbackId)) return 0;
            var messageId = _callbackIdToMessageId[callbackId];
            return _messageIdToPage.ContainsKey(messageId) ? _messageIdToPage[messageId] : 0;
        }

        public string GetOptionByOptionId(int optionId)
        {
            return _optionIdToOption[optionId];
        }

        public int GetOptionIdByOption(string option)
        {
            return _optionToOptionId[option];
        }

        public bool DatumIdHasMessage(DatumId datumId)
        {
            var callbackId = _datumIdToCallbackId[datumId.ToString()];
            return _callbackIdToMessageId.ContainsKey(callbackId);
        }

        public void NextPageByCallbackId(int callbackId)
        {
            var messageId = _callbackIdToMessageId[callbackId];
            var currentPage = _messageIdToPage[messageId];
            _messageIdToPage[messageId] = currentPage + 1;
        }

        public void PrevPageByCallbackId(int callbackId)
        {
            var messageId = _callbackIdToMessageId[callbackId];
            var currentPage = _messageIdToPage[messageId];
            _messageIdToPage[messageId] = Math.Max(currentPage - 1, 0);
        }

        public int MessageCount()
        {
            return _callbackIdToMessageId.Count;
        }

        public List<long> MessageIds()
        {
            return _callbackIdToMessageId.Values.ToList();
        }

        public void ClearMessages()
        {
            _callbackIdToMessageId.Clear();
            _messageIdToPage.Clear();
        }

        public IdIndexData ToData()
        {
            return new IdIndexData
            {
                CallbackIdToMessageId = _callbackIdToMessageId,
                CallbackIdToDatumId = _callbackIdToDatumId,
                MessageIdToPage = _messageIdToPage,
                OptionToOptionId = _optionToOptionId
            };
        }

        public class IdIndexData
        {
            public Dictionary<int, long> CallbackIdToMessageId { get; set; }
            public Dictionary<int, string> CallbackIdToDatumId { get; set; }
            public Dictionary<long, int> MessageIdToPage { get; set; }
            public Dictionary<string, int> OptionToOptionId { get; set; }
        }
    }
}