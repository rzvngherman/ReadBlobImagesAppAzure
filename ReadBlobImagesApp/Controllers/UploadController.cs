using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace ReadBlobImagesApp.Controllers
{
    public class UploadController : Controller
    {
        private readonly HttpClient _httpClient;

        private readonly IConfiguration _configuration;
        private readonly IAzureHelper _azureHelper;
        private readonly IConfigKeys _configKeys;

        public UploadController(
            ILogger<UploadController> logger,
            IConfiguration configuration,
            HttpClient httpClient,
            IAzureHelper helper,
            IConfigKeys configKeys)
        {
            _configuration = configuration;
            _configKeys = configKeys;
            _httpClient = httpClient;
            _azureHelper = helper;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(
            List<IFormFile> postedFiles,
            string containerName,
            bool shouldCreateContainerIfNotExists)
        {
            if (postedFiles == null || postedFiles.Count == 0)
            {
                @ViewBag.Message = (new ArgumentNullException(nameof(postedFiles))).Message;
                return View("~/Views/Upload/Index.cshtml");
            }

            if (string.IsNullOrEmpty(containerName))
            {
                @ViewBag.Message = (new ArgumentNullException(nameof(containerName))).Message;
                return View("~/Views/Upload/Index.cshtml");
            }

            if (shouldCreateContainerIfNotExists)
            {
                await _azureHelper.CreateContainer(containerName);
            }

            using (var multipartFormContent = new MultipartFormDataContent())
            {
                //Add other fields
                multipartFormContent.Add(new StringContent(containerName), "ContainerName");

                //Add files
                foreach (var postedFile in postedFiles)
                {
                    using var memoryStream3 = new MemoryStream();
                    await postedFile.CopyToAsync(memoryStream3);

                    //Add the file
                    var fileStreamContent3 = new ByteArrayContent(memoryStream3.ToArray());
                    fileStreamContent3.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                    multipartFormContent.Add(fileStreamContent3, name: "file", fileName: postedFile.FileName);
                }

                var response = await _httpClient.PostAsync(_configKeys.UploadUrl, multipartFormContent);
                var responseContent2 = await response.Content.ReadAsStringAsync();

                @ViewBag.Message = responseContent2;
                return View("~/Views/Upload/Index.cshtml");
                //return Ok(responseContent2);
            }
        }

        
    }
}
