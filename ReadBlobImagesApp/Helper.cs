using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Http;

namespace ReadBlobImagesApp
{
    public interface IAzureHelper
    {
        Task<Microsoft.Azure.Management.Fluent.IAzure> GetAzureSubscription();
        Task<string> GetToken();
        Task CreateContainer(string containerName);
        Task<string> GetConnectionStringFromKey1(int keyIndex);
    }

    public class AzureHelper : IAzureHelper
    {
        private readonly IConfigKeys _configKeys;
        private readonly string _createContainerUrlFormat = "https://management.azure.com/subscriptions/{0}/resourceGroups/{1}/providers/Microsoft.Storage/storageAccounts/{2}/blobServices/default/containers/{3}?api-version=2022-09-01";
        private readonly HttpClient _httpClient;

        public AzureHelper(IConfigKeys configKeys, HttpClient httpClient)
        {
            _configKeys = configKeys;
            _httpClient = httpClient;
        }

        public async Task<Microsoft.Azure.Management.Fluent.IAzure> GetAzureSubscription()
        {
            var token = await GetToken();

            var tokenCredentials = new TokenCredentials(token);
            var credentials = new AzureCredentials(
                tokenCredentials,
                tokenCredentials,
                _configKeys.TenantId,
                AzureEnvironment.AzureGlobalCloud);

            var azure = Microsoft.Azure.Management.Fluent.Azure
                            .Configure()
                            .Authenticate(credentials)
                            .WithSubscription(_configKeys.SubscriptionId)
                            //.WithClientClaims
                            ;

            return azure;
        }

        public async Task<string> GetToken()
        {
            string authContextURL = "https://login.windows.net/" + _configKeys.TenantId;
            var authenticationContext = new AuthenticationContext(authContextURL);
            if (authenticationContext is null)
            {
                throw new ArgumentNullException(nameof(authenticationContext));
            }

            var credential = new ClientCredential(_configKeys.ClientId, _configKeys.ClientSecret);

            var result = await authenticationContext.AcquireTokenAsync(
                            resource: "https://management.azure.com/",
                            clientCredential: credential);
            string token = result.AccessToken;

            return token;
        }

        public async Task CreateContainer(string containerName)
        {
            var postUrl = string.Format(_createContainerUrlFormat, _configKeys.SubscriptionId, _configKeys.ResourceGroupName, _configKeys.StorageAccountName, containerName);
            var token = await GetToken();

            using (var request2 = new HttpRequestMessage(HttpMethod.Put, postUrl))
            {
                request2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                request2.Content = new StringContent("{}");
                var response2 = await _httpClient.SendAsync(request2);

                var x1 = await response2.Content.ReadAsStringAsync();
                var x2 = response2.StatusCode.ToString();
            }
        }

        public async Task<string> GetConnectionStringFromKey1(int keyIndex)
        {
            var principalLogIn = new ServicePrincipalLoginInformation();
            principalLogIn.ClientId = _configKeys.ClientId;
            principalLogIn.ClientSecret = _configKeys.ClientSecret;

            var azure = await GetAzureSubscription();
            var keys = azure.StorageAccounts.GetByResourceGroup(_configKeys.ResourceGroupName, _configKeys.StorageAccountName).GetKeys();

            var key1 = keys[keyIndex];
            //var key2 = keys[1];

            var connStr1 = "DefaultEndpointsProtocol=https;AccountName=" + _configKeys.StorageAccountName + ";AccountKey=" + key1.Value + ";EndpointSuffix=core.windows.net";
            //var connStr2 = "DefaultEndpointsProtocol=https;AccountName=" + _storageAccountName + ";AccountKey=" + key2.Value + ";EndpointSuffix=core.windows.net";

            return connStr1;
        }
    }

    public interface IConfigKeys
    {
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        string ResourceGroupName { get; }
        string StorageAccountName { get; }
        public string UploadUrl { get; }

        public int CacheGetImagesMinutes { get; }
    }

    public class ConfigKeys : IConfigKeys
    {
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public string ResourceGroupName { get; }
        public string StorageAccountName { get; }
        public string UploadUrl { get; }
        public int CacheGetImagesMinutes { get; }

        public ConfigKeys(IConfiguration configuration)
        {
            if (configuration is null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            SubscriptionId = configuration.GetValue<string>("ConfigKeys:SubscriptionId");
            if (string.IsNullOrEmpty(SubscriptionId))
            {
                throw new ArgumentNullException(nameof(SubscriptionId));
            }

            TenantId = configuration.GetValue<string>("ConfigKeys:TenantId");
            if (string.IsNullOrEmpty(TenantId))
            {
                throw new ArgumentNullException(nameof(TenantId));
            }

            ClientId = configuration.GetValue<string>("ConfigKeys:ClientId");
            if (string.IsNullOrEmpty(ClientId))
            {
                throw new ArgumentNullException(nameof(ClientId));
            }

            ClientSecret = configuration.GetValue<string>("ConfigKeys:ClientSecret");
            if (string.IsNullOrEmpty(ClientSecret))
            {
                throw new ArgumentNullException(nameof(ClientSecret));
            }

            ResourceGroupName = configuration.GetValue<string>("ConfigKeys:StorageAccountResourceGroupName");
            if (string.IsNullOrEmpty(ResourceGroupName))
            {
                throw new ArgumentNullException(nameof(ResourceGroupName));
            }

            StorageAccountName = configuration.GetValue<string>("ConfigKeys:StorageAccountName");
            if (string.IsNullOrEmpty(StorageAccountName))
            {
                throw new ArgumentNullException(nameof(StorageAccountName));
            }

            UploadUrl = configuration.GetValue<string>("ConfigKeys:UploadFileUrl");
            if (string.IsNullOrEmpty(UploadUrl))
            {
                throw new ArgumentNullException(nameof(UploadUrl));
            }

            CacheGetImagesMinutes = configuration.GetValue<int>("ConfigKeys:CacheGetImagesMinutes");
            if (CacheGetImagesMinutes == 0)
            {
                CacheGetImagesMinutes = 480; //8 ore
            }
        }

    }

    public class CacheKeys
    {
        public static string HomeIndexResponseModel = "HomeIndexResponseModel";
    }
}
