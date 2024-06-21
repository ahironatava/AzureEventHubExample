using UserRequestEventPublisher.Models;

namespace UserRequestEventPublisher.Interfaces
{
    public interface IRequestPublisherService
    {
        public Task ProcessRequest(UserRequest userRequest);
    }
}

