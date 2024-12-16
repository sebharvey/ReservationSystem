using Res.Domain.Enums;

namespace Res.Domain.Requests
{
    public class AddNameRequest
    {
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Title { get; set; }
        public PassengerType PassengerType { get; set; } = PassengerType.Adult;
    }
}