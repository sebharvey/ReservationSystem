namespace Res.Domain.Entities.SeatMap
{
    public class CabinMap
    {
        public string CabinCode { get; set; }
        public string CabinName { get; set; }
        public List<SeatRow> Rows { get; set; } = new();
    }
}