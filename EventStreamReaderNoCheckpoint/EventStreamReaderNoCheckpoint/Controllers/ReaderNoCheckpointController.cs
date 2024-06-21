using Microsoft.AspNetCore.Mvc;
using EventStreamReaderNoCheckpoint.Interfaces;

namespace EventStreamReaderNoCheckpoint.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReaderNoCheckpointController : ControllerBase
    {
        private readonly IReaderNoCheckpointService _receiverService;
        private readonly ILogger<ReaderNoCheckpointController> _logger;

        public ReaderNoCheckpointController(IReaderNoCheckpointService readerService, ILogger<ReaderNoCheckpointController> logger)
        {
            _receiverService = readerService;
            _logger = logger;
        }

        // GET the results for the default number of seconds.
        [HttpGet]
        public async Task<string> Get()
        {
            // Call the service to fetch the request.
            string entries = await _receiverService.GetEntriesInTimebox(1);
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
                entries = await _receiverService.GetEntriesInTimebox(timeSeconds);
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
