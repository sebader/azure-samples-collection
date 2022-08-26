using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

namespace SampleFunctions
{
    public static class Blob2HttpSAS
    {
        [FunctionName("Blob2HttpSAS")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "blob/sas/{filename}")] HttpRequest req,
        [Blob("test/{filename}.pdf", FileAccess.Read)] BlobClient blobClient,
        ILogger log, string filename)
        {
            if (blobClient == null)
            {
                // TODO return error page:
                return new OkResult();
            }
            else
            {
                var sas = blobClient.GenerateSasUri(Azure.Storage.Sas.BlobSasPermissions.Read, new DateTimeOffset(DateTime.UtcNow.AddMinutes(1)));
                return new RedirectResult(sas.ToString());
            }
        }
    }
}
