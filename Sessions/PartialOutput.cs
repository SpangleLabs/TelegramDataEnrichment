using System;
using System.Collections.Generic;

namespace TelegramDataEnrichment.Sessions
{
    public class PartialOutput
    {
        public enum OutputParts
        {
            Type,
            JsonFilename,
            JsonTagKey,
            Done
        }

        private DataOutput.DataOutputTypes? _type;
        private readonly string _directoryName;
        private string _jsonFileName;
        private string _jsonTagKey;

        public PartialOutput()
        {
        }

        public PartialOutput(PartialData data)
        {
            _type = data.Type;
            _directoryName = data.DirectoryName;
            _jsonFileName = data.JsonFileName;
            _jsonTagKey = data.JsonTagKey;
        }

        public OutputParts NextPart()
        {
            switch (_type)
            {
                case null:
                    return OutputParts.Type;
                case DataOutput.DataOutputTypes.SubDirectory:
                    return OutputParts.Done;
                case DataOutput.DataOutputTypes.Json when _jsonFileName == null:
                    return OutputParts.JsonFilename;
                case DataOutput.DataOutputTypes.Json when _jsonTagKey == null:
                    return OutputParts.JsonTagKey;
                case DataOutput.DataOutputTypes.Json:
                    return OutputParts.Done;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Menu NextMenu(PartialSource partialSource)
        {
            switch (_type)
            {
                case null:
                    return new CreateSessionOutputTypeMenu(DataOutput.AllowedDataOutput(partialSource.Type));
                case DataOutput.DataOutputTypes.SubDirectory:
                    return null;
                case DataOutput.DataOutputTypes.Json when _jsonFileName == null:
                    return new CreateSessionOutputJsonFileName();
                case DataOutput.DataOutputTypes.Json when _jsonTagKey == null:
                    return new CreateSessionOutputJsonTagKey();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool WaitingForText()
        {
            var nextPart = NextPart();
            var textPartsList = new List<OutputParts> {OutputParts.JsonFilename, OutputParts.JsonTagKey};
            return textPartsList.Contains(nextPart);
        }

        public void AddText(string text)
        {
            var nextPart = NextPart();
            switch (nextPart)
            {
                case OutputParts.JsonFilename:
                    _jsonFileName = text;
                    break;
                case OutputParts.JsonTagKey:
                    _jsonTagKey = text;
                    break;
            }
        }

        public bool WaitingForCallback()
        {
            switch (_type)
            {
                case null:
                    return true;
                default:
                    return false;
            }
        }

        public void AddCallback(string callbackData)
        {
            var nextPart = NextPart();
            if (nextPart == OutputParts.Type)
            {
                switch (callbackData)
                {
                    case CreateSessionOutputTypeMenu.CallbackJson:
                        _type = DataOutput.DataOutputTypes.Json;
                        break;
                    case CreateSessionOutputTypeMenu.CallbackSubDirectory:
                        _type = DataOutput.DataOutputTypes.SubDirectory;
                        break;
                }
            }
        }

        public bool AllowMultipleOptions()
        {
            switch (_type)
            {
                case DataOutput.DataOutputTypes.SubDirectory:
                    return false;
                case DataOutput.DataOutputTypes.Json:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public DataOutput BuildOutput(string sessionName, DataSource.DataSourceData sourceData)
        {
            switch (_type)
            {
                case null:
                    throw new ArgumentException();
                case DataOutput.DataOutputTypes.SubDirectory:
                    return new SubDirectoryOutput(sourceData.DirectoryName);
                case DataOutput.DataOutputTypes.Json:
                    return new JsonOutput(sessionName, _jsonFileName, _jsonTagKey);
                default:
                    throw new ArgumentException();
            }
        }

        public PartialData ToData()
        {
            return new PartialData
            {
                Type = _type,
                DirectoryName = _directoryName,
                JsonFileName = _jsonFileName,
                JsonTagKey = _jsonTagKey
            };
        }

        public class PartialData
        {
            public DataOutput.DataOutputTypes? Type { get; set; }
            public string DirectoryName { get; set; }
            public string JsonFileName { get; set; }
            public string JsonTagKey { get; set; }
        }
    }
}