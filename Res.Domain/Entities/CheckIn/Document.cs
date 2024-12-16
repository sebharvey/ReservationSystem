namespace Res.Domain.Entities.CheckIn
{
    public class Document
    {
        public string Type { get; set; }  // PP (Passport), ID (Identity Card), VS (Visa)
        public string Number { get; set; }
        public string IssuingCountry { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime? IssueDate { get; set; }
        public string Nationality { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string StatusCode { get; set; }
        public string Surname { get; set; }
        public string Firstname { get; set; }
    }
}