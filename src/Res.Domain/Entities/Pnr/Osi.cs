using Res.Domain.Enums;

namespace Res.Domain.Entities.Pnr
{
    public class Osi
    {
        public string Id { get; set; }
        public string PnrLocator { get; set; }
        public string CompanyId { get; set; } // Airline code the information is for
        public OsiCategory Category { get; set; }
        public string SubCategory { get; set; } // Optional further categorization
        public string Text { get; set; } // Free text information
        public DateTime CreatedDate { get; set; }
        public string ElementNumber { get; set; } // Reference number in the PNR
        public bool IsConfidential { get; set; } // Whether this should be hidden from some users
        public string CreatedBy { get; set; } // User/system that created the OSI
    }
}