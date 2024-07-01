using Microsoft.AspNetCore.Mvc;
using EventStreamPublisher.Interfaces;

namespace EventStreamPublisher.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventStreamPublisherController : ControllerBase
    {
        private readonly IEventStreamPublisherService _publisherService;
        private readonly ILogger<EventStreamPublisherController> _logger;

        public EventStreamPublisherController(IEventStreamPublisherService publisherService, ILogger<EventStreamPublisherController> logger)
        {
            _publisherService = publisherService;
            _logger = logger;
        }

        // POST to send the specified count of events
        [HttpPost]
        public async void Post([FromBody] string countAsString)
        {
            int count;

            // If request is invalid return a 400 (Bad Request)
            if ((string.IsNullOrWhiteSpace(countAsString)) ||
                !int.TryParse(countAsString, out count))
            {
                Response.StatusCode = 400;
                return;
            }

            // Make a call to the service to instigate asynchronous processing of the request
            Response.StatusCode = 202;
            _ = await _publisherService.SendNEvents(count);
            return;
        }
    }
}
