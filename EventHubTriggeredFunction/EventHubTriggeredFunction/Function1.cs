using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace EventHubTriggeredFunction
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }


        [Function(nameof(EventHubFunction))]
        public string EventHubFunction(
            [EventHubTrigger("src", Connection = "EventHubConnection")] string[] input,
            FunctionContext context)
        {
            _logger.LogInformation("First Event Hubs triggered message: {msg}", input[0]);

            var message = $"Output message created at {DateTime.Now}";
            return message;
        }

    }
}
