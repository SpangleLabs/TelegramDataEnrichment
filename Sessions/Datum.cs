namespace TelegramDataEnrichment.Sessions
{
    public abstract class Datum
    {
        public string DatumId;
        public int IdNumber;
        public long MessageId;

        public abstract void Post();
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

        public override void Post()
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

        public override void Post()
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

        public override void Post()
        {
            throw new System.NotImplementedException();
        }
    }
}