﻿using System.Collections.Generic;
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
            public DataSourceTypes Type { get; set; }
            public string DirectoryName { get; set; }
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
            var files = Directory.GetFiles(_directory);
            var data = new List<Datum>();
            var datumId = 0;
            foreach (var file in files)
            {
                data.Add(Datum.FromFile(file, datumId++));
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