using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace SampleFunctions
{
    public static class QueueTriggeredFunction
    {
        [FunctionName("QueueTriggeredFunction")]
        public static void Run([ServiceBusTrigger("demofunctionqueue", Connection = "queueconstring")]string[] myQueueItems, ILogger log)
        {
            log.LogInformation("Received messages {count}", myQueueItems.Length);
            foreach (var myQueueItem in myQueueItems)
            {
                log.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            }
        }
    }
}
