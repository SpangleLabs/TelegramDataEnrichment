namespace TelegramDataEnrichment.Sessions
{
    public abstract class Datum
    {
        public int DatumId;
        public long MessageId;

        public abstract void Post();
    }

    public class TextDatum : Datum
    {
        public string Text;
        
        public TextDatum(int datumId, string text)
        {
            DatumId = datumId;
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

        public ImageDatum(int datumId, string imagePath)
        {
            DatumId = datumId;
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

        public DocumentDatum(int datumId, string documentPath)
        {
            DatumId = datumId;
            DocumentPath = documentPath;
        }

        public override void Post()
        {
            throw new System.NotImplementedException();
        }
    }
}