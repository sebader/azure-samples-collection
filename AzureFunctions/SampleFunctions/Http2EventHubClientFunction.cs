using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.EventHubs;
using System.Text;

namespace SampleFunctions
{
    public static class Http2EventHubClientFunction
    {
        private static string connectionString = Environment.GetEnvironmentVariable("EventHubConnectionAppSetting");
        private static string eventHubName = Environment.GetEnvironmentVariable("eventhubname");

        private static EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString($"{connectionString};EntityPath={eventHubName}");

        [FunctionName("Http2EventHubClientFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "v3/{method}")] HttpRequest req, 
            string method,
            ILogger log)
        {
            log.LogInformation($"Received call {method}");
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (method == "login")
            {
                var sid = new { sid = Guid.NewGuid().ToString("N") };
                string json = JsonConvert.SerializeObject(sid);
                log.LogInformation(json);
                return new OkObjectResult(sid);
            }
            else if (method == "upload")
            {
                log.LogInformation("sid:" + req.Headers["sid"]);
                log.LogInformation("body:" + requestBody);

                EventData data = new EventData(Encoding.UTF8.GetBytes(requestBody));
                await eventHubClient.SendAsync(data);
                return new OkObjectResult(null); ;
            }
            else if (method == "logout")
            {
                log.LogInformation("sid:" + req.Headers["sid"]);
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
