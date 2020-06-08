using System.IO;
using System.Net.Http;
using DreadBot;
using File = System.IO.File;

namespace TelegramDataEnrichment.Sessions
{
    public abstract class Datum
    {
        public string DatumId;
        public int IdNumber;
        public long MessageId;

        public abstract void Post(long chatId, InlineKeyboardMarkup keyboard);

        public static Datum FromFile(string fileName, int datumId)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            switch (ext)
            {
                case ".txt":
                    return new TextDatum(fileName, datumId, File.ReadAllText(fileName));
                case ".png":
                case ".jpg":
                case ".jpeg":
                    return  new ImageDatum(fileName, datumId, fileName);
                default:
                    return new DocumentDatum(fileName, datumId, fileName);
            }
        }
    }

    public class TextDatum : Datum
    {
        public string Text;
        
        public TextDatum(string datumId, int idNumber, string text)
        {
            DatumId = datumId;
            IdNumber = idNumber;
            Text = text;
        }

        public override void Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ImageDatum : Datum
    {
        public string ImagePath;

        public ImageDatum(string datumId, int idNumber, string imagePath)
        {
            DatumId = datumId;
            IdNumber = idNumber;
            ImagePath = imagePath;
        }

        public override void Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            throw new System.NotImplementedException();
        }
    }

    public class DocumentDatum : Datum
    {
        public string DocumentPath;

        public DocumentDatum(string datumId, int idNumber, string documentPath)
        {
            DatumId = datumId;
            IdNumber = idNumber;
            DocumentPath = documentPath;
        }

        public override void Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            throw new System.NotImplementedException();
        }
    }
}