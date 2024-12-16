namespace Res.Domain.Entities.SeatMap
{
    public class SeatRow
    {
        public int RowNumber { get; set; }
        public List<Seat> Seats { get; set; } = new();
    }
}