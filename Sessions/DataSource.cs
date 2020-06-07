using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TelegramDataEnrichment.Sessions
{
    public enum DataSourceTypes
    {
        DirectorySource
    }
    public abstract class DataSource
    {
        public abstract List<Datum> ListData();
        public abstract DataSourceData ToData();

        public class DataSourceData
        {
            public DataSourceTypes Type;
            public string DirectoryName;
        }

        public static DataSource FromData(DataSourceData data)
        {
            switch (data.Type)
            {
                case DataSourceTypes.DirectorySource:
                    return new DirectorySource(data);
                default:
                    return null;
            }
        }
    }

    public class DirectorySource : DataSource
    {
        private const string BaseDirectory = "input_data";
        private readonly string _directory;
        private List<Datum> _data;

        public DirectorySource(string directory)
        {
            _directory = directory;
        }

        public DirectorySource(DataSourceData data)
        {
            _directory = data.DirectoryName;
        }

        public static List<string> ListDirectories()
        {
            Directory.CreateDirectory(BaseDirectory);
            return new List<string>(Directory.GetDirectories(BaseDirectory).OrderBy(f => f));
        }
        
        public override List<Datum> ListData()
        {
            return _data ?? (_data = ParseDirectory());
        }

        private List<Datum> ParseDirectory()
        {
            var files = Directory.GetFiles(_directory);
            var data = new List<Datum>();
            var datumId = 0;
            foreach (var file in files)
            {
                var ext = Path.GetExtension(file);
                switch (ext)
                {
                    case ".txt":
                        data.Add(new TextDatum(datumId++, File.ReadAllText(file)));
                        break;
                    case ".png":
                    case ".jpg":
                    case ".jpeg":
                        data.Add(new ImageDatum(datumId++, file));
                        break;
                    default:
                        data.Add(new DocumentDatum(datumId++, file));
                        break;
                }
            }

            return data;
        }

        public override DataSourceData ToData()
        {
            return new DataSourceData
            {
                Type = DataSourceTypes.DirectorySource,
                DirectoryName = _directory
            };
        }
    }
}