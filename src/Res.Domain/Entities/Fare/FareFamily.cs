using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Res.Domain.Entities.Fare
{
    public class FareFamily
    {
        public string Code { get; set; }  // Basic, Flex, etc.
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> Benefits { get; set; } = new();
        public bool IsRefundable { get; set; }
        public bool IsChangeable { get; set; }
        public decimal ChangeFee { get; set; }
        public bool HasSeatSelection { get; set; }
        public bool HasBaggageIncluded { get; set; }
        public bool HasPriorityBoarding { get; set; }
        public bool HasLoungeAccess { get; set; }
        public decimal PriceMultiplier { get; set; } // Factor to multiply base fare by
        public decimal BaseFare { get; set; } // The base fare before multiplier
        public decimal TotalFare { get; set; } // The final fare after multiplier
    }
}
