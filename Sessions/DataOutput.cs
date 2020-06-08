using System;
using System.Collections.Generic;
using System.IO;

namespace TelegramDataEnrichment.Sessions
{
    public abstract class DataOutput
    {
        public enum DataOutputTypes
        {
            SubDirectory
        }

        public abstract List<Datum> RemoveCompleted(List<Datum> data);

        public abstract List<Datum> ListCompleted();
        
        public abstract DataOutputData ToData();

        public abstract void HandleDatum(Datum datum, string tag);

        public class DataOutputData
        {
            public DataOutputTypes Type { get; set; }
            public string DataDirectory { get; set; }
        }

        public static DataOutput FromData(DataOutputData data, DataSource.DataSourceData sourceData)
        {
            switch (data.Type)
            {
                case DataOutputTypes.SubDirectory:
                    return new SubDirectoryOutput(sourceData.DirectoryName);
                default:
                    return null;
            }
        }

        public static List<DataOutputTypes> AllowedDataOutput(DataSourceTypes dataSourceType)
        {
            switch (dataSourceType)
            {
                case DataSourceTypes.DirectorySource:
                    return new List<DataOutputTypes> {DataOutputTypes.SubDirectory};
                default:
                    throw new ArgumentOutOfRangeException(nameof(dataSourceType), dataSourceType, null);
            }
        }
    }

    public class SubDirectoryOutput : DataOutput
    {
        private string _dataDirectory;

        public SubDirectoryOutput(string dataDirectory)
        {
            _dataDirectory = dataDirectory;
        }

        public override List<Datum> RemoveCompleted(List<Datum> data)
        {
            return data;
        }

        public override List<Datum> ListCompleted()
        {
            var directories = Directory.GetDirectories(_dataDirectory);
            var datumId = 0;
            var data = new List<Datum>();
            foreach (var directory in directories)
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files)
                {
                    data.Add(Datum.FromFile(file, datumId++));
                }
            }
            return data;
        }

        public override DataOutputData ToData()
        {
            return new DataOutputData
            {
                Type = DataOutputTypes.SubDirectory,
                DataDirectory = _dataDirectory
            };
        }

        public override void HandleDatum(Datum datum, string tag)
        {
            Directory.CreateDirectory($"{_dataDirectory}/{tag}");
            File.Move(
                $"{_dataDirectory}/{datum.DatumId}",
                $"{_dataDirectory}/{tag}/{datum.DatumId}"
            );
        }
    }
}