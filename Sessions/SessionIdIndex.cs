using System;
using System.Collections.Generic;
using System.Linq;

namespace TelegramDataEnrichment.Sessions
{
    public class SessionIdIndex
    {
        private readonly Dictionary<int, long> _callbackIdToMessageId;
        private readonly Dictionary<int, string> _callbackIdToDatumId;
        private readonly Dictionary<string, int> _datumIdToCallbackId;
        private readonly Dictionary<long, int> _messageIdToPage;
        private int _nextCallbackId;

        public SessionIdIndex()
        {
            _callbackIdToMessageId = new Dictionary<int, long>();
            _callbackIdToDatumId = new Dictionary<int, string>();
            _datumIdToCallbackId = new Dictionary<string, int>();
            _messageIdToPage = new Dictionary<long, int>();
            _nextCallbackId = 0;
        }

        public SessionIdIndex(
            IdIndexData data
        )
        {
            _callbackIdToMessageId = data.CallbackIdToMessageId;
            _callbackIdToDatumId = data.CallbackIdToDatumId;
            _messageIdToPage = data.MessageIdToPage;
            _nextCallbackId = _callbackIdToDatumId.Count == 0 ? 0 : _callbackIdToDatumId.Keys.Max() + 1;
            _datumIdToCallbackId = _callbackIdToDatumId.ToDictionary((i) => i.Value, (i) => i.Key);
        }

        public void AddDatumId(string datumId)
        {
            if (_datumIdToCallbackId.ContainsKey(datumId) || _callbackIdToDatumId.ContainsValue(datumId)) return;
            var callbackId = _nextCallbackId++;
            _callbackIdToDatumId.Add(callbackId, datumId);
            _datumIdToCallbackId.Add(datumId, callbackId);
        }

        public void AddMessageId(long messageId, int callbackId)
        {
            _callbackIdToMessageId.Add(callbackId, messageId);
            _messageIdToPage.Add(messageId, 0);
        }

        public void RemoveMessageByCallbackId(int callbackId)
        {
            var messageId = _callbackIdToMessageId[callbackId];
            _callbackIdToMessageId.Remove(callbackId);
            _messageIdToPage.Remove(messageId);
        }

        public string GetDatumIdFromCallbackId(int callbackId)
        {
            return _callbackIdToDatumId[callbackId];
        }

        public int GetCallbackIdFromDatumId(string datumId)
        {
            return _datumIdToCallbackId[datumId];
        }

        public long GetMessageIdFromCallbackId(int callbackId)
        {
            return _callbackIdToMessageId[callbackId];
        }

        public int GetPageFromCallbackId(int callbackId)
        {
            if (!_callbackIdToMessageId.ContainsKey(callbackId)) return 0;
            var messageId = _callbackIdToMessageId[callbackId];
            return _messageIdToPage.ContainsKey(messageId) ? _messageIdToPage[messageId] : 0;
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
                MessageIdToPage = _messageIdToPage
            };
        }

        public class IdIndexData
        {
            public Dictionary<int, long> CallbackIdToMessageId { get; set; }
            public Dictionary<int, string> CallbackIdToDatumId { get; set; }
            public Dictionary<long, int> MessageIdToPage { get; set; }
        }
    }
}