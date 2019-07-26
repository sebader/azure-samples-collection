using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace SampleFunctions
{
    public static class QueueTriggeredFunction
    {
        [FunctionName("QueueTriggeredFunction")]
        public static void Run([ServiceBusTrigger("demofunctionqueue", Connection = "queueconstring")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
        }
    }
}
