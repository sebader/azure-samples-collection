using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Blob;

namespace SampleFunctions
{
    public static class Http2BlobFunction
    {
        [FunctionName(nameof(Http2Blob))]
        public static async Task<IActionResult> Http2Blob(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            [Blob("myblobcontainer/{rand-guid}.txt", FileAccess.ReadWrite)] CloudBlockBlob blob,
            ILogger log)
        {
            log.LogInformation("Received file upload request");
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            await blob.UploadTextAsync(requestBody);
            return new OkObjectResult(blob.Name);
        }
    }
}
