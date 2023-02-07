using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Web;

namespace ReadBlobImagesApp.Controllers
{
    public class UploadController : Controller
    {
        private readonly string _uploadUrl;

        public UploadController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _uploadUrl = configuration.GetValue<string>("ConfigKeys:UploadFileUrl");
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, string containerName)
        {
            var client = GetClient();

            if (file.Length <= 0)
            {
                return BadRequest();
            }

            using (var memoryStream = new MemoryStream())
            {
                //Get the file steam from the multiform data uploaded from the browser
                await file.CopyToAsync(memoryStream);

                //Build an multipart / form - data request to upload the file to Web API
                using var form = new MultipartFormDataContent();
                using var fileContent = new ByteArrayContent(memoryStream.ToArray());
                fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                
                form.Add(fileContent, "File", file.FileName);
                form.Add(new StringContent(containerName), "ContainerName");

                var response = await client.PostAsync(_uploadUrl, form);
                var responseContent = await response.Content.ReadAsStringAsync();
                return Ok(responseContent);
            }
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient();
            return client;
        }
    }
}
