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

        public abstract Result<Message> Post(long chatId, InlineKeyboardMarkup keyboard);

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
        private readonly string _text;
        
        public TextDatum(string datumId, int idNumber, string text)
        {
            DatumId = datumId;
            IdNumber = idNumber;
            _text = text;
        }

        public override Result<Message> Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            var result = Methods.sendMessage(chatId, _text, keyboard: keyboard);
            MessageId = result.result.message_id;
            return result;
        }
    }

    public class ImageDatum : Datum
    {
        private readonly string _imagePath;

        public ImageDatum(string datumId, int idNumber, string imagePath)
        {
            DatumId = datumId;
            IdNumber = idNumber;
            _imagePath = imagePath;
        }

        public override Result<Message> Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            var stream = File.OpenRead(_imagePath);
            var imageContent = new StreamContent(stream);
            var result = Methods.sendPhoto(chatId, imageContent, _imagePath,"", keyboard: keyboard);
            MessageId = result.result.message_id;
            return result;
        }
    }

    public class DocumentDatum : Datum
    {
        private readonly string _documentPath;

        public DocumentDatum(string datumId, int idNumber, string documentPath)
        {
            DatumId = datumId;
            IdNumber = idNumber;
            _documentPath = documentPath;
        }

        public override Result<Message> Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            var stream = File.OpenRead(_documentPath);
            var docContent = new StreamContent(stream);
            var result = Methods.sendDocument(chatId, docContent, "", keyboard: keyboard);
            MessageId = result.result.message_id;
            return result;
        }
    }
}