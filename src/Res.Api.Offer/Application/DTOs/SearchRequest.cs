namespace Res.Api.Offer.Application.DTOs
{
    public class SearchRequest
    {
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Date { get; set; }
        public List<PassengerNumber> Passengers { get; set; }
        public string Currency { get; set; }
    }

    public class PassengerNumber
    {
        public string Ptc { get; set; }
        public int Total { get; set; }
    }
}