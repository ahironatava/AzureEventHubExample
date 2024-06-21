using Microsoft.AspNetCore.Mvc;
using EventStreamReaderCheckpoint.Interfaces;

namespace EventStreamReaderCheckpoint.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReaderCheckpointController : ControllerBase
    {
        private readonly IReaderCheckpointService _readerService;
        private readonly ILogger<ReaderCheckpointController> _logger;

        public ReaderCheckpointController(IReaderCheckpointService receiverService, ILogger<ReaderCheckpointController> logger)
        {
            _readerService = receiverService;
            _logger = logger;
        }

        // GET the results for the default number of seconds.
        [HttpGet]
        public async Task<string> Get()
        {
            // Call the service to fetch the request.
            string entries = await _readerService.GetEntriesInTimebox(1);
            Response.StatusCode = 200;
            return entries;
        }

        // GET the results for the specified number of seconds.
        [HttpGet("{timeSeconds}")]
        public async Task<string> Get(int timeSeconds)
        {
            string entries = string.Empty;
            if (timeSeconds > 0)
            {
                // Call the service to fetch the request.
                entries = await _readerService.GetEntriesInTimebox(timeSeconds);
                Response.StatusCode = 200;
            }
            else
            {
                Response.StatusCode = 400;
            }

            return entries;
        }

    }
}
