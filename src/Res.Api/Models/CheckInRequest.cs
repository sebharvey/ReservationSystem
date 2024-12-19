namespace Res.Api.Models
{
    public class CheckInRequest
    {
        public string RecordLocator { get; set; }
        public string From { get; set; }
        public CheckInRequestApisInformation ApisInformation { get; set; }

        public List<CheckInPassengerInfo> PassengerInfo { get; set; }

        public class CheckInPassengerInfo
        {
            public string DocType { get; set; } // P for passport
            public string IssuingCountry { get; set; }
            public string DocNumber { get; set; }
            public string Nationality { get; set; }
            public DateTime Dob { get; set; }  
            public DateTime ExpiryDate { get; set; }
            public string Gender { get; set; }
            public int PassengerId { get; set; } // Index in which they are stored on the PNR
        }

        public class CheckInRequestApisInformation
        {
            public string Type { get; set; } // R for residence
            public string Country { get; set; }   // GBR
            public string Street { get; set; }     // 123 HIGH STREET
            public string City { get; set; }     // LONDON
            public string State { get; set; }      // GB
            public string Postal { get; set; }    // W1A 1AA
        }
    }
}
