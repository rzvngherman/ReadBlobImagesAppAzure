namespace ReadBlobImagesApp.Models
{
    public class HomeIndexResponseModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
    {
        public string ContainerName { get; set; }
        public List<string> Urls { get; set; }
    }
}
