using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using EventStreamReaderNoCheckpoint.Interfaces;
using System.Text;


namespace EventStreamReaderNoCheckpoint.Services
{
    public class ReaderNoCheckpointService : IReaderNoCheckpointService
    {
        private readonly IConfiguration _configuration;

        private BlobContainerClient _blobContainerClient;
        private EventProcessorClient _ehProcessorClient;

        private StringBuilder _receivedEventStrings;


        public ReaderNoCheckpointService(IConfiguration configuration)
        {
            _configuration = configuration;

            _blobContainerClient = CreateBlobContainerClient(configuration);
            _ehProcessorClient = CreateEventProcessorClient(configuration, _blobContainerClient);

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

        private Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            _receivedEventStrings.AppendLine(Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray()));
            return Task.CompletedTask;
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
    }
}
