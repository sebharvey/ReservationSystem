using Res.Domain.Entities.CheckIn;
using Res.Domain.Enums;

namespace Res.Domain.Entities.Pnr
{
    public class Passenger
    {
        public int PassengerId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Title { get; set; }
        public PassengerType Type { get; set; }
        public List<Document> Documents { get; set; }
    }
}