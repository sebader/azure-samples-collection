using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.EventHubs;
using System.Threading.Tasks;

namespace SampleFunctions
{
    public static class ServiceBus2EventHubFunction
    {
        [FunctionName("ServiceBus2EventHubFunction")]
        public static async Task Run([ServiceBusTrigger("%serviceBusTopicName%", "%serviceBusSubscriptionName%", Connection = "serviceBusTriggerConnection")] ServiceBusMessage [] incomingMessages,
            [EventHub("%eventhubname%", Connection = "EventHubConnectionAppSetting")] IAsyncCollector<EventData> outputEventHubMessages,
            ILogger log)
        {
            log.LogInformation($"ServiceBus topic trigger function received {incomingMessages.Length} message(s)");
            for (int i = 0; i < incomingMessages.Length; i++)
            {
                log.LogInformation($"Processing message {i}");
                ServiceBusMessage message = incomingMessages[i];
                var outboundMessage = new EventData(message.Body);
                // Copy user-defined properties
                foreach(var prop in message.ApplicationProperties)
                {
                    outboundMessage.Properties.Add(prop.Key, prop.Value);
                }
                // Add message to outbound collector
                await outputEventHubMessages.AddAsync(outboundMessage);
            }
        }
    }
}
