using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.Web.Http;

namespace FileUploadFunction
{
    public static class FileUpload
    {
        [FunctionName("FileUpload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
        {
            string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            //string containerName = Environment.GetEnvironmentVariable("ContainerName");
            string containerName = req.Form["ContainerName"][0];

            Stream myBlob = new MemoryStream();
            var file = req.Form.Files["File"];

            string fileName;
            if (file is null)
            {
                //throw new NullReferenceException(nameof(file));
                fileName = req.Form["File"][0];
            }
            else
            {
                fileName = file.FileName;
            }

            myBlob = file.OpenReadStream();
            var blobClient = new BlobContainerClient(Connection, containerName);

            //await blobClient.UploadBlobAsync("folder1/folder2/" + file.FileName, myBlob);

            var blob = blobClient.GetBlobClient(fileName);
            try
            {
                await blob.UploadAsync(myBlob);
                return new OkObjectResult("file uploaded successfylly");
            }
            catch (Exception ex)
            {
                return new OkObjectResult(ex.ToString());
            }
        }
    }
}