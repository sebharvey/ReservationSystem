using Res.Domain.Entities.Fare;

namespace Res.Domain.Entities.Pnr
{
    public class FareInfo
    {
        public int PassengerId { get; set; }
        public string FareBasis { get; set; }
        public string ValidatingCarrier { get; set; }
        public decimal FareAmount { get; set; }
        public string Currency { get; set; }
        public bool IsStored { get; set; }
        public string FareRestrictions { get; set; }
        public Dictionary<string, string> FareRules { get; set; } = new();
        public DateTime? LastDateToTicket { get; set; }
        // Add new properties
        public string FareFamilyCode { get; set; }
        public List<FareFamily> AvailableFareFamilies { get; set; } = new();
        public int SelectedFareOption { get; set; } = 1; // Default to cheapest
    }
}