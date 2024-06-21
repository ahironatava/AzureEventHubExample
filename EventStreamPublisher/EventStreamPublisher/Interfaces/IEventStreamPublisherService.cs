namespace EventStreamPublisher.Interfaces
{
    public interface IEventStreamPublisherService
    {
        public Task<string> SendNEvents(int count);
    }
}
