namespace UserRequestEventPublisher.Models
{
    public class UserRequest
    {
        public string? UserId { get; set; }
        public string? RequestId { get; set; }
        public string? RequestType { get; set; }
        public string[]? RequestParameterList { get; set; }
    }
}

