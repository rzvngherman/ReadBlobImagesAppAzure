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
        private const string UPLOAD_VIEW_PATH = "~/Views/Upload/Index.cshtml";

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
            HttpContext.Session.SetString("StorageAccountName", _configKeys.StorageAccountName);
            //TempData["StorageAccountName"] = _configKeys.StorageAccountName;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(
            List<IFormFile> postedFiles,
            string containerName,
            bool shouldCreateContainerIfNotExists,
            bool isOverwriting)
        {
            if (postedFiles == null || postedFiles.Count == 0)
            {
                @ViewBag.Message = ReadMessageByLang(1001, new ArgumentNullException(nameof(postedFiles)).Message);
                return View(UPLOAD_VIEW_PATH);
            }

            if (string.IsNullOrEmpty(containerName))
            {
                @ViewBag.Message = ReadMessageByLang(1002, new ArgumentNullException(nameof(containerName)).Message);
                return View(UPLOAD_VIEW_PATH);
            }

            if (shouldCreateContainerIfNotExists)
            {
                await _azureHelper.CreateContainer(containerName);
            }

            using (var multipartFormContent = new MultipartFormDataContent())
            {
                //Add other fields
                multipartFormContent.Add(new StringContent(containerName), "ContainerName");
                multipartFormContent.Add(new StringContent(isOverwriting.ToString()), "Overwrite");

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

                if (!response.IsSuccessStatusCode)
                {
                    @ViewBag.Message = ReadMessageByLang((int)response.StatusCode, responseContent2);
                    return View(UPLOAD_VIEW_PATH);
                }

                @ViewBag.Message = ReadMessageByLang(2001, responseContent2);

                return View(UPLOAD_VIEW_PATH);
            }
        }

        private string ReadMessageByLang(int messageIndex, string message)
        {
            if (messageIndex == 1001)
            {
                //"Value cannot be null. (Parameter 'postedFiles')"
                return "Va rog selectati fisierele pt upload";
            }
            if (messageIndex == 1002)
            {
                // "Value cannot be null. (Parameter 'containerName')"
                return "Va rog introduceti 'container name'";
            }

            
            if (messageIndex == 409 && message.Equals("BlobAlreadyExists"))
            {
                //409
                //"BlobAlreadyExists"
                return "Fisierul(le) incarcate deja exista";
            }

            return message;
        }
    }
}
