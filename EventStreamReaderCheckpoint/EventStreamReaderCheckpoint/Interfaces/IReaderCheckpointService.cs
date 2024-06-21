namespace EventStreamReaderCheckpoint.Interfaces
{
    public interface IReaderCheckpointService
    {
        public Task<string> GetEntriesInTimebox(int timeSeconds);
    }
}

