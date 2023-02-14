using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using ReadBlobImagesApp.Models;
using System.Diagnostics;

namespace ReadBlobImagesApp.Controllers
{
    public class GetImagesController : Controller
    {
        private readonly ILogger<GetImagesController> _logger;

        private string _connectionString = "";
        private int? _segmentSize = null;
        private BlobServiceClient _blobServiceClient;

        private readonly IAzureHelper _azureHelper;
        private readonly IConfigKeys _configKeys;

        private readonly string[] _excludeContainerNames = new[] { "azure-webjobs-hosts", "azure-webjobs-secrets" };

        public GetImagesController(
            ILogger<GetImagesController> logger,
            IAzureHelper helper,
            IConfigKeys configKeys)
        {
            _logger = logger;
            _azureHelper = helper;
            _configKeys = configKeys;

            _connectionString = ReadConnectionString();
            _blobServiceClient = new BlobServiceClient(_connectionString);
        }

        #region IActionResult
        public IActionResult Index()
        {
            var containerItems = _blobServiceClient
                                    .GetBlobContainers()
                                    .Where(b => !_excludeContainerNames.Contains(b.Name));

            var contents = new List<HomeIndexResponseModel>();

            foreach (var containerItem in containerItems)
            {
                var blobContainerClient = new BlobContainerClient(_connectionString, containerItem.Name);

                var urls = GetImageUrls(blobContainerClient);

                var content = new HomeIndexResponseModel
                {
                    ContainerName = containerItem.Name,
                    Urls = urls,
                };
                contents.Add(content);
            }

            return View(contents);
        }

        public IActionResult CreateZip(string containerName)
        {
            var containerItem = _blobServiceClient
                                    .GetBlobContainers(BlobContainerTraits.Metadata)
                                    .First(b => b.Name == containerName);

            BlobContainerClient contianner = new BlobContainerClient(_connectionString, containerItem.Name);

            var list = contianner.GetBlobs();
            var zipName = $"{containerItem.Name}.zip";

            using (MemoryStream fs = new MemoryStream())
            {
                using (ZipOutputStream zipOutputStream = new ZipOutputStream(fs))
                {
                    foreach (var blob2 in list)
                    {
                        zipOutputStream.SetLevel(0);
                        BlobClient blockBlob2 = contianner.GetBlobClient(blob2.Name);

                        var entry = new ZipEntry(blob2.Name);
                        zipOutputStream.PutNextEntry(entry);
                        blockBlob2.DownloadTo(zipOutputStream);
                    }

                    zipOutputStream.Finish();
                    zipOutputStream.Close();

                    return File(fs.ToArray(), "application/zip", zipName);
                }
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        #endregion

        private string ReadConnectionString()
        {
            var connectionString = "";
            var task = Task.Run(async () => { connectionString = await _azureHelper.GetConnectionStringFromKey1(0); });
            task.Wait();

            return connectionString;
        }

        private List<string> GetImageUrls(BlobContainerClient blobContainerClient)
        {
            var urls = new List<string>();
            try
            {
                // Call the listing operation and return pages of the specified size.
                var resultSegment = blobContainerClient.GetBlobs();
                //.AsPages(default, _segmentSize);

                foreach (var blobItem in resultSegment)
                {
                    var url = DownloadImage(blobItem.Name, blobContainerClient);
                    urls.Add(url);
                }

                return urls;
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
        }

        private string DownloadImage(string blobName, BlobContainerClient blobContainerClient)
        {
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

            MemoryStream memoryStream = new MemoryStream();

            // Ensure our client has the credentials required to generate a SAS
            if (blobClient.CanGenerateSasUri)
            {
                // Create full, self-authenticating URI to the resource from the BlobClient
                var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1));

                // Use newly made as SAS URI to download the blob
                //await new BlobClient(sasUri).DownloadToAsync(new MemoryStream());
                new BlobClient(sasUri).DownloadTo(memoryStream);
                return sasUri.ToString();
            }

            //else
            blobClient.DownloadTo(memoryStream);
            memoryStream.Position = 0;

            return blobClient.Uri.ToString();
        }
    }
}