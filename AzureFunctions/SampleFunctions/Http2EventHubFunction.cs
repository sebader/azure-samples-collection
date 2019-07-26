using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SampleFunctions
{
    public static class Http2EventHubFunction
    {
        [FunctionName("Http2EventHubFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "v2/{method}")] HttpRequest req, 
            string method,
            [EventHub("functiondemo", Connection = "EventHubConnectionAppSetting")]IAsyncCollector<string> outputEventHubMessages,
            ILogger log)
        {
            log.LogInformation($"Received call {method}");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            if (method == "login")
            {
                var sid = new { sid = Guid.NewGuid().ToString("N") };
                string json = JsonConvert.SerializeObject(sid);
                log.LogInformation(json);
                await outputEventHubMessages.AddAsync("{\"method\":\"login\"}");
                return new OkObjectResult(sid);
            }
            else if (method == "upload")
            {
                log.LogInformation("sid:" + req.Headers["sid"]);
                log.LogInformation("body:" + requestBody);

                await outputEventHubMessages.AddAsync(requestBody);
                return new OkObjectResult(null); ;
            }
            else if (method == "logout")
            {
                log.LogInformation("sid:" + req.Headers["sid"]);
                await outputEventHubMessages.AddAsync("{\"method\":\"logout\"}");
                return new OkObjectResult(null);
            }
            else
            {
                var error = $"Error: unknown method '{method}'";
                log.LogInformation(error);
                return new NotFoundObjectResult(error);
            }
        }
    }
}
