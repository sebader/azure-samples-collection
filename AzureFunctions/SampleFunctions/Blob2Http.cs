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
    public static class Blob2Http
    {
        [FunctionName("Blob2Http")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "blob/{filename}")] HttpRequest req,
        [Blob("test/{filename}.pdf", FileAccess.Read)] byte[] blobContent,
        ILogger log, string filename)
        {
            if (blobContent == null)
            {
                // TODO return error page:
                return new OkResult();
            }
            else
            {
                try
                {
                    return new FileContentResult(blobContent, MediaTypeNames.Application.Pdf)
                    {
                        FileDownloadName = $"{filename}.pdf"
                    };
                }
                catch (Exception e)
                {
                    log.LogError(e, "Error returning blobcontent");
                    throw;
                }
            }
        }
    }
}
