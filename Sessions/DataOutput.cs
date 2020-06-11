using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        public abstract void HandleDatumDone(Datum datum);
        public abstract List<string> GetOptionsForData(Datum datum);

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

        public static List<DataOutputTypes> AllowedDataOutput(DataSourceTypes? dataSourceType)
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
        private readonly string _dataDirectory;

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
            var data = new List<Datum>();
            foreach (var directory in directories)
            {
                var files = Directory.GetFiles(directory);
                data.AddRange(files.Select(Datum.FromFile));
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
            var fileDatum = (FileDatum) datum;
            Directory.CreateDirectory($"{_dataDirectory}/{tag}");
            File.Move(
                fileDatum.FileName,
                $"{_dataDirectory}/{tag}/{Path.GetFileName(fileDatum.FileName)}"
            );
        }

        public override void HandleDatumDone(Datum datum)
        {
        }

        public override List<string> GetOptionsForData(Datum datum)
        {
            if(!(datum is FileDatum fileDatum)) return new List<string>();
            var directories = Directory.GetDirectories(_dataDirectory);
            var fileName = Path.GetFileName(fileDatum.FileName);
            foreach (var directory in directories)
            {
                var files = Directory.GetFiles(directory).Select(Path.GetFileName);
                if (files.Contains(fileName)) return new List<string> {Path.GetDirectoryName(directory)};
            }

            return new List<string>();
        }
    }
}