using Res.Domain.Enums;

namespace Res.Domain.Entities.Pnr
{
    public class Ssr
    {
        public int Id { get; set; }
        public string PnrLocator { get; set; }
        public int PassengerId { get; set; } // Can be null if applies to all passengers
        public int SegmentNumber { get; set; } // Can be null if applies to all segments
        public SsrType Type { get; set; }
        public SsrStatus Status { get; set; }
        public string Code { get; set; } // WCHR, VGML, etc.
        public string Text { get; set; } // Free text description
        public bool IsAutoConfirmed { get; set; } // Some SSRs auto-confirm, others need airline confirmation
        public DateTime CreatedDate { get; set; }
        public string ActionCode { get; set; } // NN (Need), HK (Confirmed), NO (No Action), etc.
        public string CompanyId { get; set; } // Airline code that will action the SSR
        public int Quantity { get; set; } // Number of items/services requested

        // For equipment/special handling
        public string Equipment { get; set; } // Optional: specific equipment details
        public string Specifications { get; set; } // Optional: additional specifications
    }
}