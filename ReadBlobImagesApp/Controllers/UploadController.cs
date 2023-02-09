using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;

namespace ReadBlobImagesApp.Controllers
{
    public class UploadController : Controller
    {
        private readonly string _uploadUrl;

        public UploadController(ILogger<UploadController> logger, IConfiguration configuration)
        {
            _uploadUrl = configuration.GetValue<string>("ConfigKeys:UploadFileUrl");
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(List<IFormFile> postedFiles, string containerName)
        {
            if(postedFiles == null)
            {
                throw new ArgumentNullException(nameof(postedFiles));
            }

            var client = GetClient();

            using (var multipartFormContent = new MultipartFormDataContent())
            {
                //Add other fields
                multipartFormContent.Add(new StringContent(containerName), "ContainerName");

                //Add files
                foreach (var postedFile in postedFiles)
                {
                    //add file 01
                    using var memoryStream3 = new MemoryStream();
                    await postedFile.CopyToAsync(memoryStream3);
                    //Add the file
                    var fileStreamContent3 = new ByteArrayContent(memoryStream3.ToArray());
                    fileStreamContent3.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    multipartFormContent.Add(fileStreamContent3, name: "file", fileName: postedFile.FileName);
                }

                var response = await client.PostAsync(_uploadUrl, multipartFormContent);
                var responseContent2 = await response.Content.ReadAsStringAsync();
                return Ok(responseContent2);
            }
        }

        private HttpClient GetClient()
        {
            var client = new HttpClient();
            return client;
        }
    }
}
