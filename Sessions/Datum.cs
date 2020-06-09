using System.IO;
using System.Net.Http;
using DreadBot;
using File = System.IO.File;

namespace TelegramDataEnrichment.Sessions
{
    public abstract class Datum
    {
        public string DatumId;  // This wants to be an external unique ID

        public abstract Result<Message> Post(long chatId, InlineKeyboardMarkup keyboard);

        public static Datum FromFile(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            switch (ext)
            {
                case ".txt":
                    return new TextDatum(fileName, File.ReadAllText(fileName));
                case ".png":
                case ".jpg":
                case ".jpeg":
                    return  new ImageDatum(fileName, fileName);
                default:
                    return new DocumentDatum(fileName, fileName);
            }
        }
    }

    public class TextDatum : Datum
    {
        private readonly string _text;
        
        public TextDatum(string datumId, string text)
        {
            DatumId = datumId;
            _text = text;
        }

        public override Result<Message> Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            return Methods.sendMessage(chatId, _text, keyboard: keyboard);
        }
    }

    public class ImageDatum : Datum
    {
        private readonly string _imagePath;

        public ImageDatum(string datumId, string imagePath)
        {
            DatumId = datumId;
            _imagePath = imagePath;
        }

        public override Result<Message> Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            var stream = File.OpenRead(_imagePath);
            var imageContent = new StreamContent(stream);
            return Methods.sendPhoto(chatId, imageContent, _imagePath,"", keyboard: keyboard);
        }
    }

    public class DocumentDatum : Datum
    {
        private readonly string _documentPath;

        public DocumentDatum(string datumId, string documentPath)
        {
            DatumId = datumId;
            _documentPath = documentPath;
        }

        public override Result<Message> Post(long chatId, InlineKeyboardMarkup keyboard)
        {
            var stream = File.OpenRead(_documentPath);
            var docContent = new StreamContent(stream);
            return Methods.sendDocument(chatId, docContent, "", keyboard: keyboard);
        }
    }
}