using Microsoft.AspNetCore.Mvc;
using UserRequestEventPublisher.Interfaces;
using UserRequestEventPublisher.Models;

namespace UserRequestEventPublisher.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestPublisherController : ControllerBase
    {
        private readonly IRequestPublisherService _requestPublisherService;
        private readonly ILogger<RequestPublisherController> _logger;

        public RequestPublisherController(IRequestPublisherService requestPublisherService, ILogger<RequestPublisherController> logger)
        {
            _requestPublisherService = requestPublisherService;
            _logger = logger;
        }

        [HttpPost]
        public void SendEvent([FromBody] UserRequest userRequest)
        {
            _logger.LogInformation("Received request from client {ClientId} for request {RequestId} of type {RequestType}", userRequest.UserId, userRequest.RequestId, userRequest.RequestType);

            if (!ValidateRequest(userRequest))
            {
                Response.StatusCode = 400;
                return;
            }

            // Call the service asynchronous method but do not wait for it to complete
            Response.StatusCode = 202;
            _ = _requestPublisherService.ProcessRequest(userRequest);

            return;
        }

        private bool ValidateRequest(UserRequest userRequest)
        {
            bool valid = true;

            if (string.IsNullOrWhiteSpace(userRequest.UserId)
                || string.IsNullOrWhiteSpace(userRequest.RequestId)
                || string.IsNullOrWhiteSpace(userRequest.RequestType)
                )
            {
                valid = false;
            }

            return valid;
        }

    }
}
