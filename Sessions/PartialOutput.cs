using System;

namespace TelegramDataEnrichment.Sessions
{
    public class PartialOutput
    {

        public enum OutputParts
        {
            Type,
            Done
        }

        private DataOutput.DataOutputTypes? _type;
        private readonly string _directoryName;

        public PartialOutput()
        {
            
        }

        public PartialOutput(PartialData data)
        {
            _type = data.Type;
            _directoryName = data.DirectoryName;
        }
        
        public OutputParts NextPart()
        {
            switch (_type)
            {
                case null:
                    return OutputParts.Type;
                case DataOutput.DataOutputTypes.SubDirectory:
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
                    return new CreateSessionOutputTypeMenu(partialSource.ToData().Type);
                case DataOutput.DataOutputTypes.SubDirectory:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool WaitingForText()
        {
            return false;
        }

        public void AddText(string text)
        {
            throw new ArgumentOutOfRangeException();
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
                    case CreateSessionOutputTypeMenu.CallbackSubDirectory:
                        _type = DataOutput.DataOutputTypes.SubDirectory;
                        break;
                }
            }
        }

        public DataOutput BuildOutput(DataSource.DataSourceData sourceData)
        {
            if (_type == null)
            {
                throw new ArgumentException();
            }

            if (_type == DataOutput.DataOutputTypes.SubDirectory)
            {
                return new SubDirectoryOutput(sourceData.DirectoryName);
            }
            throw new ArgumentException();
        }

        public PartialData ToData()
        {
            return new PartialData
            {
                Type = _type, 
                DirectoryName = _directoryName
            };
        }

        public class PartialData
        {
            public DataOutput.DataOutputTypes? Type { get; set; }
            public string DirectoryName { get; set; }
        }
    }
}