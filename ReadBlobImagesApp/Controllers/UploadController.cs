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
        private readonly IMessageHelper _messageHelper;

        private const string UPLOAD_VIEW_PATH = "~/Views/Upload/Index.cshtml";

        public UploadController(
            ILogger<UploadController> logger,
            IConfiguration configuration,
            HttpClient httpClient,
            IAzureHelper helper,
            IConfigKeys configKeys,
            IMessageHelper messageHelper)
        {
            _configuration = configuration;
            _configKeys = configKeys;
            _httpClient = httpClient;
            _azureHelper = helper;
            _messageHelper = messageHelper;
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
                @ViewBag.Message = _messageHelper.GetByIndex(1001);
                return View(UPLOAD_VIEW_PATH);
                //return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            if (string.IsNullOrEmpty(containerName))
            {
                @ViewBag.Message = _messageHelper.GetByIndex(1002);
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
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    @ViewBag.Message = ReadMessageFromUploadCalls(responseContent, (int)response.StatusCode);
                    return View(UPLOAD_VIEW_PATH);
                }

                @ViewBag.Message = responseContent;
                return View(UPLOAD_VIEW_PATH);
            }
        }

        private string ReadMessageFromUploadCalls(string message, int messageIndex)
        {
            if (messageIndex == StatusCodes.Status409Conflict && message.Equals("BlobAlreadyExists"))
            {
                return _messageHelper.GetByIndex(4091);
            }

            //TODO add more errors from post url data

            return message;
        }
    }
}
