using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SampleFunctions
{
    public static class TimerTriggeredFunction
    {
        // This Function is currently disabled by an app setting and does not execute
        [FunctionName("TimerTriggeredFunction")]
        public static void Run([TimerTrigger("0 */1 * * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
