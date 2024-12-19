namespace Res.Domain.Entities.CheckIn
{
    public class PassengerApis
    {
        public int PassengerId { get; set; }

        // Travel Document
        public string DocumentType { get; set; }  // "P" for passport
        public string DocumentNumber { get; set; }
        public string DocumentIssuingCountry { get; set; }
        public DateTime DocumentExpiryDate { get; set; }

        // Personal Information
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Nationality { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }

        // Residence Information
        public string CountryOfResidence { get; set; }
        public string ResidenceAddress { get; set; }
        public string ResidenceCity { get; set; }
        public string ResidenceState { get; set; }
        public string ResidencePostalCode { get; set; }

        // Destination Information
        public string DestinationAddress { get; set; }
        public string DestinationCity { get; set; }
        public string DestinationState { get; set; }
        public string DestinationPostalCode { get; set; }
    }
}