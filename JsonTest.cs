using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace TelegramDataEnrichment
{
    [TestFixture]
    public class JsonTest
    {
        private const string SessionsDoneKey = "__sessions_completed";

        class JsonDataOutputException : EnrichmentException
        {
            public JsonDataOutputException(string fault)
                : base($"Fault in JSON data output: {fault}")
            {
            }

            public JsonDataOutputException(string datumId, string message)
                : base($"Fault in JSON data output with Datum ID: \"{datumId}\". Fault: {message}")
            {
            }
        }

        private static string ReadJsonFile(string fileName)
        {
            string jsonString;
            try
            {
                jsonString = File.ReadAllText(fileName);
            }
            catch (FileNotFoundException)
            {
                jsonString = "{}";
            }
            catch (DirectoryNotFoundException)
            {
                jsonString = "{}";
            }

            return ReadJsonString(jsonString);
        }

        private static string ReadJsonString(string jsonString)
        {
            var dataFile = ParseJsonString(jsonString);

            ValidateDatumEntry(dataFile, "example.png", "tags");

            //File.WriteAllText(fileName, dataFile.ToString());
            return dataFile.ToString();
        }

        private static JObject ParseJsonString(string jsonString)
        {
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

        private static void ValidateDatumEntry(JObject dataFile, string datumId, string tagsKey)
        {
            JObject datumEntry;
            try
            {
                datumEntry = (JObject) dataFile[datumId];
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
                dataFile[datumId] = datumEntry;
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
                    $"Failure to parse data entry, datum ID already has a {SessionsDoneKey} key " +
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
                tagList = (JArray) datumEntry[tagsKey];
            }
            catch (InvalidCastException)
            {
                throw new JsonDataOutputException(
                    datumId,
                    $"Failure to parse data entry, datum ID already has a {tagsKey} key " +
                    "(the key the bot intends to use to store the output of this enrichment session) " +
                    "pointing to a non-array value in the output file."
                );
            }

            if (tagList == null)
            {
                tagList = new JArray();
                datumEntry[tagsKey] = tagList;
            }
        }

        [Test]
        public void NoFile()
        {
            const string fileName = "./JsonTestResources/not_a_file.json";
            Console.WriteLine(ReadJsonFile(fileName));
        }

        [Test]
        public void EmptyFile()
        {
            const string fileName = "./JsonTestResources/empty_file.json";
            Console.WriteLine(ReadJsonFile(fileName));
        }

        [Test]
        public void NullString()
        {
            const string jsonString = null;
            Console.WriteLine(ReadJsonString(jsonString));
        }

        [Test]
        public void EmptyString()
        {
            const string jsonString = "";
            Console.WriteLine(ReadJsonString(jsonString));
        }

        [Test]
        public void TextString()
        {
            const string jsonString = "hello world";
            Assert.Throws<JsonDataOutputException>(() => Console.WriteLine(ReadJsonString(jsonString)));
        }

        [Test]
        public void ArrayString()
        {
            const string jsonString = "[1,2,3]";
            Assert.Throws<JsonDataOutputException>(() =>
                Console.WriteLine(ReadJsonString(jsonString))
            );
        }

        [Test]
        public void EmptyObject()
        {
            const string jsonString = "{}";
            Console.WriteLine(ReadJsonString(jsonString));
        }

        [Test]
        public void ObjectWithDatumKey()
        {
            const string jsonString = "{\"example.png\":{}}";
            Console.WriteLine(ReadJsonString(jsonString));
        }

        [Test]
        public void ObjectWithDatumKeyAndOthers()
        {
            const string jsonString = "{\"example.png\":{},\"other.jpg\":{}}";
            Console.WriteLine(ReadJsonString(jsonString));
        }

        [Test]
        public void ObjectWithDatumKeyToArray()
        {
            const string jsonString = "{\"example.png\":[]}";
            Assert.Throws<JsonDataOutputException>(() => Console.WriteLine(ReadJsonString(jsonString)));
        }

        [Test]
        public void ObjectWithDatumKeyToString()
        {
            const string jsonString = "{\"example.png\":\"hello world\"}";
            Assert.Throws<JsonDataOutputException>(() => Console.WriteLine(ReadJsonString(jsonString)));
        }

        [Test]
        public void ObjectWithDatumKeyAndRequiredKeys()
        {
            const string jsonString = "{\"example.png\":{\"__sessions_completed\":[], \"tags\":[]}}";
            Console.WriteLine(ReadJsonString(jsonString));
        }

        [Test]
        public void ObjectWithDatumKeyAndRequiredKeysToPartialArrays()
        {
            const string jsonString =
                "{\"example.png\":{\"__sessions_completed\":[\"hello\",\"world\"], \"tags\":[\"greeting\",\"planet\"]}}";
            Console.WriteLine(ReadJsonString(jsonString));
        }

        [Test]
        public void ObjectWithDatumKeyAndRequiredKeysToObject()
        {
            const string jsonString = "{\"example.png\":{\"__sessions_completed\":{}, \"tags\":{}}}";
            Assert.Throws<JsonDataOutputException>(() => Console.WriteLine(ReadJsonString(jsonString)));
        }

        [Test]
        public void ObjectWithDatumKeyAndRequiredKeysToString()
        {
            const string jsonString =
                "{\"example.png\":{\"__sessions_completed\":\"hello world\", \"tags\":\"greeting planet\"}}";
            Assert.Throws<JsonDataOutputException>(() => Console.WriteLine(ReadJsonString(jsonString)));
        }

        [Test]
        public void ObjectWithDatumKeyAndRequiredKeysAndOthers()
        {
            const string jsonString =
                "{\"example.png\":{\"__sessions_completed\":[], \"tags\":[], \"content\":{}, \"message\":\"hi\"}}";
            Console.WriteLine(ReadJsonString(jsonString));
        }


        /*
         * Test cases:
    - no file
    - empty file
    - text file
    - file with json array inside
    -file with json object, no keys
    - file with json object, matching key to empty object
    - file with json object, matching key and other keys to empty objects
    - file with json object, matching keys to array
    - file with json object, matching keys to string
    - file with json object, matching keys to object with required keys, to empty arrays
    - file with json object, matching keys to object with required keys, to partially filled arrays
    - file with json object, matching keys to object with required keys but to objects
    - file with json object, matching keys to object with required keys, but to strings
    - file with json object, matching keys to object with required keys to arrays, but also other keys
         */
    }
}