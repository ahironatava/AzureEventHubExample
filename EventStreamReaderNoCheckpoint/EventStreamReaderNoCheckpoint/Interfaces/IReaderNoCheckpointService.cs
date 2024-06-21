namespace EventStreamReaderNoCheckpoint.Interfaces
{
    public interface IReaderNoCheckpointService
    {
        public Task<string> GetEntriesInTimebox(int timeSeconds);
    }
}
