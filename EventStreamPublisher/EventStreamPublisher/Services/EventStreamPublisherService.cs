using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using EventStreamPublisher.Interfaces;
using Microsoft.Extensions.Logging;

namespace EventStreamPublisher.Services
{
    public class EventStreamPublisherService : IEventStreamPublisherService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EventStreamPublisherService> _logger;

        private EventHubProducerClient _ehProducerClient;

        public EventStreamPublisherService(IConfiguration configuration, ILogger<EventStreamPublisherService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Create the Event Hub Producer Client
            string? hubNamesapce = _configuration["ehns_connstring"];
            string? hubName = _configuration["eh_name"];

            if (string.IsNullOrWhiteSpace(hubNamesapce) || string.IsNullOrWhiteSpace(hubName))
            {
                throw new ArgumentException("Event Hub Namespace and Hub Name must be provided.");
            }

            _ehProducerClient = new EventHubProducerClient(hubNamesapce, hubName);
            _logger = logger;
        }

        public async Task<string> SendNEvents(int count)
        {
            // Based on code at
            // https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs/samples/Sample01_HelloWorld.md

            string errMsg = string.Empty;

            try
            {
                using EventDataBatch eventBatch = await _ehProducerClient.CreateBatchAsync();

                for (var counter = 0; counter < count; ++counter)
                {
                    _logger.LogInformation($"Adding event {counter} of {count}");

                    var eventBody = new BinaryData($"Event Number: {counter}");
                    var eventData = new EventData(eventBody);
                    eventData.Properties["EventType"] = "EventStream";

                    if (!eventBatch.TryAdd(eventData))
                    {
                        // At this point, the batch is full but our last event was not
                        // accepted.  For our purposes, the event is unimportant so we
                        // will intentionally ignore it.  In a real-world scenario, a
                        // decision would have to be made as to whether the event should
                        // be dropped or published on its own.
                        _logger.LogError($"Failed to add {counter} of {count}");

                        break;
                    }
                }

                // When the producer publishes the event, it will receive an
                // acknowledgment from the Event Hubs service; so long as there is no
                // exception thrown by this call, the service assumes responsibility for
                // delivery.  Your event data will be published to one of the Event Hub
                // partitions, though there may be a (very) slight delay until it is
                // available to be consumed.

                _logger.LogInformation($"Sending batch");
                await _ehProducerClient.SendAsync(eventBatch);
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
                _logger.LogError($"Exception: {errMsg}");
            }

            return (errMsg);
        }

    }
}
