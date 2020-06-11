using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TelegramDataEnrichment.Sessions
{
    public abstract class DataOutput
    {
        public enum DataOutputTypes
        {
            SubDirectory,
            Json
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
            public string DataFilename { get; set; }
            public string JsonTagsKey { get; set; }
        }

        public static DataOutput FromData(string sessionName, DataOutputData data, DataSource.DataSourceData sourceData)
        {
            switch (data.Type)
            {
                case DataOutputTypes.SubDirectory:
                    return new SubDirectoryOutput(sourceData.DirectoryName);
                case DataOutputTypes.Json:
                    return new JsonOutput(sessionName, data.DataFilename, data.JsonTagsKey);
                default:
                    return null;
            }
        }

        public static List<DataOutputTypes> AllowedDataOutput(DataSourceTypes? dataSourceType)
        {
            switch (dataSourceType)
            {
                case DataSourceTypes.DirectorySource:
                    return new List<DataOutputTypes> {DataOutputTypes.SubDirectory, DataOutputTypes.Json};
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

    public class JsonOutput : DataOutput
    {
        private readonly string _sessionName;
        private readonly string _fileName;
        private readonly string _tagsKey;
        private const string SessionsDoneKey = "__sessions_completed";
        private readonly JsonDataHandler _handler;

        public JsonOutput(string sessionName, string fileName, string tagsKey)
        {
            _sessionName = sessionName;
            _fileName = fileName;
            _tagsKey = tagsKey;
            _handler = new JsonDataHandler(fileName, tagsKey);
        }
        
        public override List<Datum> RemoveCompleted(List<Datum> data)
        {
            var completed = ListCompleted();
            return data.Where(d => !completed.Contains(d)).ToList();
        }

        public override List<Datum> ListCompleted()
        {
            return new List<Datum>(_handler.ListCompleted(_sessionName));
        }

        public override DataOutputData ToData()
        {
            return new DataOutputData
            {
                Type = DataOutputTypes.Json,
                DataFilename = _fileName,
                JsonTagsKey = _tagsKey
            };
        }

        public override void HandleDatum(Datum datum, string option)
        {
            var dataFile = _handler.SetupDatumEntry(datum.DatumId);
            
            var optionsList = _handler.GetOptionsList(dataFile, datum.DatumId);
            if (optionsList.Any(t => t.ToString().Equals(option)))
            {
                optionsList.Where(t => t.ToString().Equals(option)).ToList().ForEach(t => optionsList.Remove(t));
            }
            else
            {
                optionsList.Add(option);
            }

            _handler.WriteFile(dataFile);
        }

        public override void HandleDatumDone(Datum datum)
        {
            var dataFile = _handler.SetupDatumEntry(datum.DatumId);
            
            var sessionsCompleteList = _handler.GetSessionsCompletedList(dataFile, datum.DatumId);
            sessionsCompleteList.Add(_sessionName);
            
            _handler.WriteFile(dataFile);
        }

        public override List<string> GetOptionsForData(Datum datum)
        {
            var dataFile = _handler.SetupDatumEntry(datum.DatumId);
            return _handler.GetOptionsList(dataFile, datum.DatumId).ToObject<List<string>>();
        }

        class JsonDataOutputException : Exception
        {
            public JsonDataOutputException(string fault)
                : base($"Fault in JSON data output: {fault}")
            {
            }

            public JsonDataOutputException(DatumId datumId, string message)
                : base($"Fault in JSON data output with Datum ID: \"{datumId}\". Fault: {message}")
            {
            }
        }

        private class JsonDataHandler
        {
            private readonly string _fileName;
            private readonly string _tagsKey;

            internal JsonDataHandler(string fileName, string tagsKey)
            {
                _fileName = fileName;
                _tagsKey = tagsKey;
                ParseJsonFile();  // Validate
            }
            
            private string ReadJsonFile()
            {
                string jsonString;
                try
                {
                    jsonString = File.ReadAllText(_fileName);
                }
                catch (FileNotFoundException)
                {
                    jsonString = "{}";
                }
                catch (DirectoryNotFoundException)
                {
                    jsonString = "{}";
                }

                return jsonString;
            }

            private JObject ParseJsonFile()
            {
                var jsonString = ReadJsonFile();
                if (jsonString == null || jsonString.Equals("")) jsonString = "{}";
                try
                {
                    return JObject.Parse(jsonString);
                }
                catch (JsonException ex)
                {
                    throw new JsonDataOutputException($"Exception while parsing JSON: {ex.Message}");
                }
            }

            internal JObject SetupDatumEntry(DatumId datumId)
            {
                var dataFile = ParseJsonFile();
                JObject datumEntry;
                try
                {
                    datumEntry = (JObject) dataFile[datumId.ToString()];
                }
                catch (InvalidCastException)
                {
                    throw new JsonDataOutputException(
                        datumId,
                        "Failure to parse data entry, datum ID already points to a non-object value in the output file."
                    );
                }

                if (datumEntry == null)
                {
                    datumEntry = new JObject();
                    dataFile[datumId.ToString()] = datumEntry;
                }

                JArray sessionsDoneList;
                try
                {
                    sessionsDoneList = (JArray) datumEntry[SessionsDoneKey];
                }
                catch (InvalidCastException)
                {
                    throw new JsonDataOutputException(
                        datumId,
                        $"Failure to parse data entry, datum ID already has a \"{SessionsDoneKey}\" key " +
                        "(the key the bot intends to use to store which enrichment sessions are completed) " +
                        "pointing to a non-array value in the output file."
                    );
                }

                if (sessionsDoneList == null)
                {
                    sessionsDoneList = new JArray();
                    datumEntry[SessionsDoneKey] = sessionsDoneList;
                }

                JArray tagList;
                try
                {
                    tagList = (JArray) datumEntry[_tagsKey];
                }
                catch (InvalidCastException)
                {
                    throw new JsonDataOutputException(
                        datumId,
                        $"Failure to parse data entry, datum ID already has a \"{_tagsKey}\" key " +
                        "(the key the bot intends to use to store the output of this enrichment session) " +
                        "pointing to a non-array value in the output file."
                    );
                }

                if (tagList == null)
                {
                    tagList = new JArray();
                    datumEntry[_tagsKey] = tagList;
                }

                return dataFile;
            }

            internal JArray GetOptionsList(JObject dataFile, DatumId datumId)
            {
                return (JArray) dataFile[datumId.ToString()]?[_tagsKey];
            }

            internal JArray GetSessionsCompletedList(JObject dataFile, DatumId datumId)
            {
                return (JArray) dataFile[datumId.ToString()]?[SessionsDoneKey];
            }

            internal IEnumerable<JsonDatum> ListCompleted(string sessionName)
            {
                var dataFile = ParseJsonFile();
                var matchingDatumIds = new List<JsonDatum>();
                foreach (var pair in dataFile)
                {
                    var datumEntry = (JObject) pair.Value;
                    if ((datumEntry[SessionsDoneKey] ?? new JArray()).Any(t => t.ToString().Equals(sessionName)))
                    {
                        matchingDatumIds.Add(Datum.FromJson(new DatumId(pair.Key), datumEntry));
                    }
                }

                return matchingDatumIds;
            }

            internal void WriteFile(JObject dataFile)
            {
                if (!File.Exists(_fileName))
                {
                    var parent = Directory.GetParent(_fileName);
                    Directory.CreateDirectory(parent.FullName);
                }
                File.WriteAllText(_fileName, dataFile.ToString());
            }
        }
    }
}