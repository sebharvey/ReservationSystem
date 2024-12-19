using Res.Domain.Enums;

namespace Res.Domain.Requests
{
    public class StoreFareRequest
    {
        public Dictionary<PassengerType, int> FareSelections { get; set; } = new();
        public bool IsDefaultSelection { get; set; }
    }
}
