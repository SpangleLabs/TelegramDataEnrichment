using System;
using System.Collections.Generic;

namespace TelegramDataEnrichment.Sessions
{
    public class PartialSource
    {

        public enum SourceParts
        {
            Type,
            DirectoryName,
            Done
        }

        public DataSourceTypes? Type;
        private string _directoryName;
        private List<string> _listPotentialDirectories;

        public PartialSource()
        {
            
        }

        public PartialSource(PartialData data)
        {
            Type = data.Type;
            _directoryName = data.DirectoryName;
        }

        private List<string> ListPotentialDirectories()
        {
            return _listPotentialDirectories ?? (_listPotentialDirectories = DirectorySource.ListDirectories());
        }
        
        public SourceParts NextPart()
        {
            switch (Type)
            {
                case null:
                    return SourceParts.Type;
                case DataSourceTypes.DirectorySource when _directoryName == null:
                    return SourceParts.DirectoryName;
                case DataSourceTypes.DirectorySource:
                    return SourceParts.Done;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public Menu NextMenu()
        {
            switch (Type)
            {
                case null:
                    return new CreateSessionDataSourceTypeMenu();
                case DataSourceTypes.DirectorySource when _directoryName == null:
                    return new CreateSessionDataSourceDirectoryName(ListPotentialDirectories());
                case DataSourceTypes.DirectorySource:
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
            switch (Type)
            {
                case null:
                    return true;
                case DataSourceTypes.DirectorySource when _directoryName == null:
                    return true;
                default:
                    return false;
            }
        }

        public void AddCallback(string callbackData)
        {
            var nextPart = NextPart();
            if (nextPart == SourceParts.Type)
            {
                switch (callbackData)
                {
                    case CreateSessionDataSourceTypeMenu.CallbackDirectorySource:
                        Type = DataSourceTypes.DirectorySource;
                        break;
                }
            }

            if (nextPart == SourceParts.DirectoryName)
            {
                var dirNumber = int.Parse(callbackData.Split(':')[1]);
                var directories = ListPotentialDirectories();
                _directoryName = directories[dirNumber];
            }
        }

        public DataSource BuildSource()
        {
            if (Type == null)
            {
                throw new ArgumentException();
            }

            if (Type == DataSourceTypes.DirectorySource)
            {
                if (_directoryName == null)
                {
                    throw new ArgumentException();
                }
                else
                {
                    return new DirectorySource(_directoryName);
                }
            }
            throw new ArgumentException();
        }

        public PartialData ToData()
        {
            return new PartialData()
            {
                Type = Type, 
                DirectoryName = _directoryName
            };
        }

        public class PartialData
        {
            public DataSourceTypes? Type { get; set; }
            public string DirectoryName { get; set; }
        }
    }
}