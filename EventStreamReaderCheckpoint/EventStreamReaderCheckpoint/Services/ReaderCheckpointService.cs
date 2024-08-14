using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using EventStreamReaderCheckpoint.Interfaces;
using System.Collections.Concurrent;
using System.Text;

namespace EventStreamReaderCheckpoint.Services
{
    public class ReaderCheckpointService : IReaderCheckpointService
    {
        private readonly IConfiguration _configuration;

        private BlobContainerClient _blobContainerClient;
        private EventProcessorClient _ehProcessorClient;

        private const int _defaultEventsBeforeCheckpoint = 10;
        private readonly int _eventsBeforeCheckpoint;
        private ConcurrentDictionary<string, int> _partitionEventCount;

        private StringBuilder _receivedEventStrings;


        public ReaderCheckpointService(IConfiguration configuration)
        {
            _configuration = configuration;

            _blobContainerClient = CreateBlobContainerClient(configuration);
            _ehProcessorClient = CreateEventProcessorClient(configuration, _blobContainerClient);

            (_eventsBeforeCheckpoint, _partitionEventCount) = InitialiseCheckpointing(configuration);

            _receivedEventStrings = new StringBuilder();
        }

        public async Task<string> GetEntriesInTimebox(int timeSeconds)
        {
            // Based on code at 
            // https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-dotnet-standard-getstarted-send?tabs=connection-string%2Croles-azure-portal

            _receivedEventStrings.Clear();

            // Wait for a timebox of the specified number of seconds for the events to be processed
            await _ehProcessorClient.StartProcessingAsync();
            await Task.Delay(TimeSpan.FromSeconds(timeSeconds));
            await _ehProcessorClient.StopProcessingAsync();

            return _receivedEventStrings.ToString();
        }

        private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            try
            {
                AddEventStringToProcessingResults(eventArgs);
                await UpdateCheckpointing(eventArgs);
            }
            catch (Exception ex)
            {
                _receivedEventStrings.AppendLine($"Exception: {ex.Message} when processing above event");
            }
        }

        private Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            _receivedEventStrings.AppendLine($"Partition '{eventArgs.PartitionId}': Unhandled exception {eventArgs.Exception.Message}.");
            return Task.CompletedTask;
        }

        private BlobContainerClient CreateBlobContainerClient(IConfiguration configuration)
        {
            // Create the Blob Container Client
            string? storageAccconnStr = configuration["storageacc_connstr"];
            string? blobContainerName = configuration["blobcontainername"];

            if (string.IsNullOrWhiteSpace(storageAccconnStr) || string.IsNullOrWhiteSpace(blobContainerName))
            {
                throw new ArgumentException("Storage Account and Blob Container Name must be provided.");
            }

            return new BlobContainerClient(storageAccconnStr, blobContainerName);
        }

        private EventProcessorClient CreateEventProcessorClient(IConfiguration configuration, BlobContainerClient blobContainerClient)
        {
            string? consumerGroupName = _configuration["consumergroup"];
            string? hubNamespace = _configuration["ehns_connstring"];
            string? hubName = _configuration["eh_name"];

            if (string.IsNullOrWhiteSpace(hubNamespace) || string.IsNullOrWhiteSpace(hubName))
            {
                throw new ArgumentException("Event Hub Namespace and Hub Name must be provided.");
            }

            EventProcessorClient ehProcessorClient = new EventProcessorClient(
                                        blobContainerClient,
                                        consumerGroupName,
                                        hubNamespace,
                                        hubName);

            // Register Processor Client handlers for events and errors
            ehProcessorClient.ProcessEventAsync += ProcessEventHandler;
            ehProcessorClient.ProcessErrorAsync += ProcessErrorHandler;

            return ehProcessorClient;
        }

        private (int, ConcurrentDictionary<string, int>) InitialiseCheckpointing(IConfiguration configuration)
        {
            var requiredEventsBeforeCheckpoint = _configuration["eventsBeforeCheckpoint"];
            if ((!int.TryParse(requiredEventsBeforeCheckpoint, out int eventsBeforeCheckpoint)) ||
                eventsBeforeCheckpoint <= 0)
            {
                eventsBeforeCheckpoint = _defaultEventsBeforeCheckpoint;
            }

            var partitionEventCount = new ConcurrentDictionary<string, int>();

            return (eventsBeforeCheckpoint, partitionEventCount);
        }

        private async Task UpdateCheckpointing(ProcessEventArgs eventArgs)
        {
            // For each event received, increment the count of events for the partition that it was found in.
            // If the event count for that partition is at the threshold value, perform a checkpoint and reset
            // the count.
            string partition = eventArgs.Partition.PartitionId;

            int eventsSinceLastCheckpoint = _partitionEventCount.AddOrUpdate(
                key: partition,
                addValue: 1,
                updateValueFactory: (_, currentCount) => currentCount + 1);

            if (eventsSinceLastCheckpoint >= _eventsBeforeCheckpoint)
            {
                await eventArgs.UpdateCheckpointAsync();
                _partitionEventCount[partition] = 0;
            }
        }

        private void AddEventStringToProcessingResults(ProcessEventArgs eventArgs)
        {
            try
            {
                var eventBody = Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray());

                var eventProperties = eventArgs.Data.Properties;
                var eventType = eventProperties["EventType"];
                var eventTypeString = eventType.ToString();
                var partitionId = eventArgs.Partition.PartitionId;
                _receivedEventStrings.AppendLine($"Partition: {partitionId}, Event Type: {eventTypeString}, Event Body: {eventBody}");
            }
            catch (KeyNotFoundException ex)
            {
                _receivedEventStrings.AppendLine($"Exception: {ex.Message} when processing above event");
            }
        }
    }
}
