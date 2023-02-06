using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using ReadBlobImagesApp.Models;
using System.Diagnostics;
using System.Text;

namespace ReadBlobImagesApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private string _connectionString = "";
        private int? _segmentSize = null;
        private BlobServiceClient _blobServiceClient;

        private string _subscriptionId = "";

        //portal -> App registrations
        private string TenantId = "";
        private string ClientId = "";
        private string ClientSecret = "";

        private string _storageAccountName = "";
        private string _resourceGroupName = "";

        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration)
        {
            _logger = logger;

            _configuration = configuration;
            ReadConfigKeys();
            
            _connectionString = ReadConnectionString();
            _blobServiceClient = new BlobServiceClient(_connectionString);
        }

        private void ReadConfigKeys()
        {
            _subscriptionId = _configuration.GetValue<string>("ConfigKeys:SubscriptionId");
            if (string.IsNullOrEmpty(_subscriptionId))
            {
                throw new ArgumentNullException(nameof(_subscriptionId));
            }

            TenantId = _configuration.GetValue<string>("ConfigKeys:TenantId");
            if (string.IsNullOrEmpty(TenantId))
            {
                throw new ArgumentNullException(nameof(TenantId));
            }

            ClientId = _configuration.GetValue<string>("ConfigKeys:ClientId");
            if (string.IsNullOrEmpty(ClientId))
            {
                throw new ArgumentNullException(nameof(ClientId));
            }

            ClientSecret = _configuration.GetValue<string>("ConfigKeys:ClientSecret");
            if (string.IsNullOrEmpty(ClientSecret))
            {
                throw new ArgumentNullException(nameof(ClientSecret));
            }

            _storageAccountName = _configuration.GetValue<string>("ConfigKeys:StorageAccountName");
            if (string.IsNullOrEmpty(_storageAccountName))
            {
                throw new ArgumentNullException(nameof(_storageAccountName));
            }

            _resourceGroupName = _configuration.GetValue<string>("ConfigKeys:StorageAccountResourceGroupName");
            if (string.IsNullOrEmpty(_resourceGroupName))
            {
                throw new ArgumentNullException(nameof(_resourceGroupName));
            }
        }

        private string ReadConnectionString()
        {
            var connectionString = "";
            var task = Task.Run(async () => { connectionString = await GetConnectionStringFromKey1(); });
            task.Wait();
            
            return connectionString;
        }

        private async Task<string> GetConnectionStringFromKey1(int keyIndex = 0)
        {
            var principalLogIn = new ServicePrincipalLoginInformation();
            principalLogIn.ClientId = ClientId;
            principalLogIn.ClientSecret = ClientSecret;

            var token = await GetToken();

            var tokenCredentials = new TokenCredentials(token);
            var credentials = new AzureCredentials(
                tokenCredentials,
                tokenCredentials,
                TenantId,
                AzureEnvironment.AzureGlobalCloud);

            var azure = Microsoft.Azure.Management.Fluent.Azure
                            .Configure()
                            .Authenticate(credentials)
                            .WithSubscription(_subscriptionId);

            var keys = azure.StorageAccounts.GetByResourceGroup(_resourceGroupName, _storageAccountName).GetKeys();

            var key1 = keys[keyIndex];
            //var key2 = keys[1];

            var connStr1 = "DefaultEndpointsProtocol=https;AccountName=" + _storageAccountName + ";AccountKey=" + key1.Value + ";EndpointSuffix=core.windows.net";
            //var connStr2 = "DefaultEndpointsProtocol=https;AccountName=" + _storageAccountName + ";AccountKey=" + key2.Value + ";EndpointSuffix=core.windows.net";
            
            return connStr1;
        }

        private async Task<string> GetToken()
        {
            string authContextURL = "https://login.windows.net/" + TenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            if (authenticationContext is null)
            {
                throw new ArgumentNullException(nameof(authenticationContext));
            }

            var credential = new ClientCredential(ClientId, ClientSecret);
            
            var result = await authenticationContext.AcquireTokenAsync(
                            resource: "https://management.azure.com/",
                            clientCredential: credential);
            string token = result.AccessToken;

            return token;
        }

        private List<string> GetImageUrls(BlobContainerClient blobContainerClient)
        {
            List<string> urls = new List<string>();
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
            blobClient.DownloadTo(memoryStream);
            memoryStream.Position = 0;

            return blobClient.Uri.ToString();
        }

        #region IActionResult
        public IActionResult Index()
        {
            var containerItems = _blobServiceClient.GetBlobContainers(BlobContainerTraits.Metadata)
                                                        .Where(b => b.Properties.PublicAccess.HasValue
                                                                && b.Properties.PublicAccess != PublicAccessType.None);

            List<HomeIndexResponseModel> contents = new List<HomeIndexResponseModel>();

            foreach (var containerItem in containerItems)
            {
                BlobContainerClient blobContainerClient = new BlobContainerClient(_connectionString, containerItem.Name);

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

        public IActionResult Privacy()
        {
            return View();
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
    }
}