namespace Res.Api.Models
{
    public class SeatMapResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FlightNumber { get; set; }
        public DateTime DepartureDate { get; set; }
        public string AircraftType { get; set; }
        public List<CabinInfo> Cabins { get; set; } = new();


        public class CabinInfo
        {
            public string CabinCode { get; set; }
            public string CabinName { get; set; }
            public List<RowInfo> Rows { get; set; }
        }

        public class RowInfo
        {
            public int RowNumber { get; set; }
            public List<SeatInfo> Seats { get; set; }
        }

        public class SeatInfo
        {
            public string SeatNumber { get; set; }
            public bool IsAvailable { get; set; }
            public bool IsExit { get; set; }
            public bool IsBulkhead { get; set; }
            public bool IsAisle { get; set; }
            public bool IsWindow { get; set; }
            public string BlockedReason { get; set; }
        }
    }
}
