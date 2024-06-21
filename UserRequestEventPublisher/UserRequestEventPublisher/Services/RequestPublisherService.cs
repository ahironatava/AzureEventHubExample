using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using UserRequestEventPublisher.Interfaces;
using UserRequestEventPublisher.Models;
using System.Text.Json;

namespace UserRequestEventPublisher.Services
{
    public class RequestPublisherService : IRequestPublisherService
    {
        private readonly IConfiguration _configuration;

        private EventHubProducerClient _ehProducerClient;
        private SendEventOptions _sendEventOptions;

        public RequestPublisherService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Create the Event Hub Producer Client
            string? hubNamesapce = _configuration["ehns_connstring"];
            string? hubName = _configuration["eh_name"];

            if (string.IsNullOrWhiteSpace(hubNamesapce) || string.IsNullOrWhiteSpace(hubName))
            {
                throw new ArgumentException("Event Hub Namespace and Hub Name must be provided.");
            }
            _ehProducerClient = new EventHubProducerClient(hubNamesapce, hubName);

            // Set the Event Hub Partition ID to send the event to
            string? hubpartitionid = _configuration["eh_partition_id"];
            if (string.IsNullOrWhiteSpace(hubpartitionid))
            {
                hubpartitionid = "0";
            }
            _sendEventOptions = new SendEventOptions { PartitionId = hubpartitionid };
        }

        public async Task ProcessRequest(UserRequest userRequest)
        {
            // Demonstrate use of specifying partition to use:
            // If the RequestType is "sell" then use partion 1, otherwise use partition 0
            if (string.Equals(userRequest.RequestType.ToLower(), "sell"))
            {
                _sendEventOptions.PartitionId = "1";
            }
            else
            {
                _sendEventOptions.PartitionId = "0";
            }
            var requestAsJson = JsonSerializer.Serialize(userRequest);
            var eventBody = new BinaryData(requestAsJson);

            var eventData = new EventData(eventBody);
            eventData.Properties["EventType"] = "UserRequest";

            var eventList = new List<EventData> { eventData };
            await _ehProducerClient.SendAsync(eventList, _sendEventOptions);
        }
    }   
}

