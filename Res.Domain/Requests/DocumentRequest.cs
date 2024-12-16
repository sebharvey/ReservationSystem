namespace Res.Domain.Requests
{
    public class DocumentRequest
    {
        public string DocumentType { get; set; }
        public string IssuingCountry { get; set; }
        public string DocumentNumber { get; set; }
        public string Nationality { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
    }
}