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
            //upload multiple files
            if (req.ContentLength == 0)
            {
                string badResponseMessage = $"Request has no content";
                return new BadRequestObjectResult(badResponseMessage);
            }

            string Connection = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            string containerName = req.Form["ContainerName"][0];

            var blobClient = new BlobContainerClient(Connection, containerName);

            if (req.Form.Files.Count == 0)
            {
                string badResponseMessage = $"No files on request";
                return new BadRequestObjectResult(badResponseMessage);
            }

            //if (req.ContentType.Contains("multipart/form-data") && req.Form.Files.Count > 0)
            try
            {
                foreach (var file in req.Form.Files)
                {
                    var fileName = file.FileName;

                    var myBlob = file.OpenReadStream();

                    //await blobClient.UploadBlobAsync("folder1/folder2/" + file.FileName, myBlob);

                    var blob = blobClient.GetBlobClient(fileName);
                    await blob.UploadAsync(myBlob);
                }
            }
            catch (Exception ex)
            {
                return new OkObjectResult(ex.ToString());
            }

            return new OkObjectResult("file(s) uploaded successfylly");
        }
    }
}