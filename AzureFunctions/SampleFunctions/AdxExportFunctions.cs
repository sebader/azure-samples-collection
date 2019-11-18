using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Kusto.Data;
using Kusto.Data.Net.Client;
using Kusto.Data.Common;
using Kusto.Cloud.Platform.Data;
using System.Linq;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Threading;
using SendGrid.Helpers.Mail;
using Newtonsoft.Json;

namespace SampleFunctions
{
    public static class AdxExportFunctions
    {
        private readonly static string adxClusterUrl = Environment.GetEnvironmentVariable("AdxExportClusterUrl");
        private readonly static string adxDatabaseName = Environment.GetEnvironmentVariable("AdxExportDatabaseName");
        private readonly static string storageConnectionString = Environment.GetEnvironmentVariable("AdxExportStorageConnectionString");

        private static KustoConnectionStringBuilder Kcsb = null;
        private static async Task<KustoConnectionStringBuilder> GetKustoConnectionStringBuilder()
        {
            if (Kcsb != null)
                return Kcsb;

            var token = await new AzureServiceTokenProvider().GetAccessTokenAsync(adxClusterUrl);
            Kcsb = new KustoConnectionStringBuilder(adxClusterUrl).WithAadApplicationTokenAuthentication(token);
            return Kcsb;
        }

        [FunctionName(nameof(AdxExportFunctionHttpTriggered))]
        public static async Task<IActionResult> AdxExportFunctionHttpTriggered(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        ILogger log,
        [DurableClient] IDurableOrchestrationClient starter)
        {
            log.LogInformation("AdxExportFunction processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var exportRequest = JsonConvert.DeserializeObject<AdxExportRequest>(requestBody);
            if (exportRequest == null || string.IsNullOrEmpty(exportRequest.AdxQuery) || string.IsNullOrEmpty(exportRequest.UserEmailAddress))
            {
                return new BadRequestObjectResult("Invalid ADX export request. Check your parameters");
            }

            using (var client = KustoClientFactory.CreateCslAdminProvider(await GetKustoConnectionStringBuilder()))
            {
                // TODO: Write actual query from user input
                var exportQuery = CslCommandGenerator.GenerateExportCommand(new[] { storageConnectionString }, "datatable1 | take 100", true, true);
                var resultReader = new ObjectReader<DataExportToBlobCommandResult>(client.ExecuteControlCommand(adxDatabaseName, exportQuery));

                var res = resultReader?.FirstOrDefault();
                var adxExportOperationId = res?.Path;

                if (!string.IsNullOrEmpty(adxExportOperationId))
                {
                    // Start durable orchestrator for the status checking
                    var durableInstanceId = await starter.StartNewAsync(nameof(AdxExportOrchestrator), new Tuple<string, string>(adxExportOperationId, exportRequest.UserEmailAddress));
                    return new OkObjectResult("Request Accepted. Durable Instance=" + durableInstanceId);
                }
                else
                {
                    log.LogError("Path is empty in result");
                    log.LogError(JsonConvert.SerializeObject(res));
                }
            }
            return new BadRequestObjectResult("Error on starting ADX export");
        }

        public class AdxExportRequest
        {
            public string UserEmailAddress { get; set; }
            public string AdxQuery { get; set; }
        }

        [FunctionName(nameof(AdxExportOrchestrator))]
        public static async Task AdxExportOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);

            var input = context.GetInput<Tuple<string, string>>();
            string path = await context.CallActivityAsync<string>(nameof(AdxExportStatusCheck), input.Item1);
            if (path == null)
            {
                // If export not yet completed, we create timer (=sleep) til we retry
                var nextCheck = context.CurrentUtcDateTime.AddSeconds(10);
                log.LogInformation($"Export not completed yet. Next check at {nextCheck.ToString("o")}");

                await context.CreateTimer(nextCheck, CancellationToken.None);
                context.StartNewOrchestration(nameof(AdxExportOrchestrator), input);
            }
            else if (path == "Error")
            {
                log.LogError("Could not retrieve export result. Giving up");
            }
            else
            {
                log.LogInformation("Retrieve path from export. Sending email to user now");
                await context.CallActivityAsync(nameof(SendCompletionEmail), new Tuple<string, string>(path, input.Item2));
            }
        }

        [FunctionName(nameof(AdxExportStatusCheck))]
        public static async Task<string> AdxExportStatusCheck(
            [ActivityTrigger] string operationId, ILogger log)
        {
            using (var client = KustoClientFactory.CreateCslAdminProvider(await GetKustoConnectionStringBuilder()))
            {
                var operationQuery = CslCommandGenerator.GenerateOperationsShowCommand(Guid.Parse(operationId));
                var resultReader = new ObjectReader<OperationsShowCommandResult>(client.ExecuteControlCommand(adxDatabaseName, operationQuery));

                var res = resultReader?.FirstOrDefault();
                var state = res?.State;
                if (state == "Completed")
                {
                    // When the state is completed, we can query the export details which contains the path to the file on blob storage
                    var operationDetailsQuery = CslCommandGenerator.GenerateOperationDetailsShowCommand(Guid.Parse(operationId));
                    var resultReader2 = new ObjectReader<DataExportToBlobCommandResult>(client.ExecuteControlCommand(adxDatabaseName, operationDetailsQuery));

                    var res2 = resultReader2?.FirstOrDefault();
                    var path = res2?.Path;
                    return path;
                }
                else if (state == "Cancelled")
                {
                    return "Error";
                }
                else
                {
                    return null;
                }
            }
        }

        [FunctionName(nameof(SendCompletionEmail))]
        public static async Task SendCompletionEmail(
            [ActivityTrigger] Tuple<string, string> input,
            [SendGrid(ApiKey = "SendGridApiKey")] IAsyncCollector<SendGridMessage> messageCollector,
        ILogger log)
        {
            log.LogInformation($"Sending email to {input.Item2}");
            var message = new SendGridMessage();
            message.AddTo(input.Item2);
            message.AddContent("text/html", $"Your ADX export job is finished. You can download you export file at {input.Item1}");
            message.SetFrom("adxexporter@sample.com");
            message.SetSubject("ADX Export Completed!");

            await messageCollector.AddAsync(message);
        }
    }
}
