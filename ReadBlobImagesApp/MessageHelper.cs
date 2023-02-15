namespace ReadBlobImagesApp
{
    public interface IMessageHelper
    {
        string GetByIndex(int messageIndex);
    }

    public class MessageHelper : IMessageHelper
    {
        public string GetByIndex(int index)
        {
            if (index == 1001)
            {
                //"Value cannot be null. (Parameter 'postedFiles')"
                return "Vă rog selectați fișierele pt upload";
            }

            if (index == 1002)
            {
                // "Value cannot be null. (Parameter 'containerName')"
                return "Vă rog introduceți 'container name'";
            }

            if (index == 4091)
            {
                //httpclient upload error (error from function)
                //"BlobAlreadyExists"
                return "Fișierele încarcate există deja";
            }

            throw new Exception($"Message index ('{index}') not found");
        }
    }
}
