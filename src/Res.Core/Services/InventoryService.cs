using System.Globalization;
using Res.Core.Common.Helpers;
using Res.Core.Interfaces;
using Res.Domain.Entities.Inventory;
using Res.Domain.Requests;
using Res.Infrastructure.Interfaces;

namespace Res.Core.Services
{
    public class InventoryService : IInventoryService
    {
        private readonly IInventoryRepository _inventoryRepository;
        private readonly ISeatMapRepository _seatMapRepository;
        private readonly Dictionary<string, List<FlightSeatInventory>> _seatInventory = new();

        public InventoryService(IInventoryRepository inventoryRepository, ISeatMapRepository seatMapRepository)
        {
            _inventoryRepository = inventoryRepository;
            _seatMapRepository = seatMapRepository;
        }

        public Task<List<FlightInventory>> SearchAvailability(AvailabilityRequest request)
        {
            // Filter flights based on criteria
            var availableFlights = _inventoryRepository.Inventory
                .Where(flight =>
                    flight.From == request.Origin &&
                    flight.To == request.Destination &&
                    flight.DepartureDate == request.DepartureDate)
                .ToList();

            // Filter out past flights if searching for today
            if (DateTimeHelper.ParseAirlineDate(request.DepartureDate) == DateTime.Today)
            {
                var currentTime = DateTime.Now.TimeOfDay;
                availableFlights = availableFlights.Where(flight =>
                {
                    var flightTime = TimeSpan.ParseExact(flight.DepartureTime, "hhmm", CultureInfo.InvariantCulture);
                    return flightTime >= currentTime;
                }).ToList();
            }

            if (request.PreferredTime != null)
            {
                availableFlights = availableFlights.Where(item => item.DepartureTime == request.PreferredTime).ToList();
            }

            // Order by departure time
            availableFlights = availableFlights.OrderBy(flight => TimeSpan.ParseExact(flight.DepartureTime, "hhmm", CultureInfo.InvariantCulture)).ToList();

            return Task.FromResult(availableFlights.Any() ? availableFlights : new List<FlightInventory>());
        }

        public async Task<FlightInventory> FindFlight(string flightNo, string date)
        {
            return _inventoryRepository.Inventory.FirstOrDefault(item => item.FlightNo == flightNo && item.DepartureDate == date);
        }

        public void AddFlight(string flightFlightNo, string flightFrom, string flightTo, string departureDate, string arrivalDate, string departureTime, string arrivalTime, Dictionary<string, int> flightSeats, string aircraftType)
        {
            _inventoryRepository.Inventory.Add(new FlightInventory
            {
                DepartureDate = departureDate,
                DepartureTime = departureTime,
                ArrivalDate = arrivalDate,
                ArrivalTime = arrivalTime,
                FlightNo = flightFlightNo,
                From = flightFrom,
                To = flightTo,
                Seats = flightSeats,
                AircraftType = aircraftType
            });
        }

        public bool IncrementInventory(string flightNumber, string date, string bookingClass, int quantity)
        {
            var flight = _inventoryRepository.Inventory.FirstOrDefault(f => f.FlightNo == flightNumber);
            if (flight == null) return false;

            // Check if booking class exists
            if (!flight.Seats.ContainsKey(bookingClass))
                return false;

            // Increment inventory
            flight.Seats[bookingClass] += quantity;

            return true;
        }

        public bool DecrementInventory(string flightNumber, string departureDate, string bookingClass, int quantity)
        {
            var flight = _inventoryRepository.Inventory.FirstOrDefault(f => f.FlightNo == flightNumber);
            if (flight == null) return false;

            // Check if booking class exists and has enough seats
            if (!flight.Seats.ContainsKey(bookingClass) || flight.Seats[bookingClass] < quantity)
                return false;

            // Decrement inventory
            flight.Seats[bookingClass] -= quantity;

            return true;
        }

        public async Task<bool> IsValidSeat(string flightNumber, string departureDate, string seatNumber)
        {
            // Get flight details
            var flight = _inventoryRepository.Inventory.FirstOrDefault(f =>
                f.FlightNo == flightNumber && f.DepartureDate == departureDate);

            if (flight == null)
                return false;

            // Get aircraft configuration
            var seatConfig = _seatMapRepository.SeatConfigurations[flight.AircraftType];

            // Parse seat number (e.g., "4D" into row 4 and letter "D")
            if (!int.TryParse(string.Join("", seatNumber.TakeWhile(char.IsDigit)), out int row))
                return false;

            string letter = new string(seatNumber.SkipWhile(char.IsDigit).ToArray());

            // Find the cabin this seat belongs to
            var cabin = seatConfig.Cabins.Values.FirstOrDefault(c =>
                row >= c.FirstRow &&
                row <= c.LastRow &&
                c.SeatLetters.Contains(letter));

            return cabin != null;
        }

        public async Task<CabinConfiguration> GetCabinConfigForSeat(string aircraftType, string seatNumber)
        {
            var seatConfig = _seatMapRepository.SeatConfigurations[aircraftType];

            if (!int.TryParse(string.Join("", seatNumber.TakeWhile(char.IsDigit)), out int row))
                return null;

            string letter = new string(seatNumber.SkipWhile(char.IsDigit).ToArray());

            return seatConfig.Cabins.Values.FirstOrDefault(c =>
                row >= c.FirstRow &&
                row <= c.LastRow &&
                c.SeatLetters.Contains(letter));
        }

        public async Task<List<FlightStatus>> FlightStatus(DateTime today)
        {
            // Get all of today's flights and any that have departed in the last 12 hours
            var recentFlights = _inventoryRepository.Inventory
                .Where(f =>
                {
                    var flightDate = DateTime.ParseExact(f.DepartureDate, "ddMMM", CultureInfo.InvariantCulture);
                    var departureDateTime = DateTimeHelper.CombineDateAndTime(f.DepartureDate, f.DepartureTime);
                    var arrivalDateTime = DateTimeHelper.CombineDateAndTime(f.ArrivalDate, f.ArrivalTime);

                    // Include flights:
                    // 1. Departing today
                    // 2. Arriving today
                    // 3. That departed in the last 12 hours
                    //return flightDate.Date == today.Date ||
                    //       arrivalDateTime.Date == today.Date ||
                    //       (departureDateTime < today && departureDateTime > today.AddHours(-12));

                    return flightDate.Date == today.Date;
                })
                .OrderBy(f => DateTimeHelper.CombineDateAndTime(f.DepartureDate, f.DepartureTime))
                .ToList();

            var flightStatuses = new List<FlightStatus>();

            foreach (var flight in recentFlights)
            {
                var departureDateTime = DateTimeHelper.CombineDateAndTime(flight.DepartureDate, flight.DepartureTime);
                var arrivalDateTime = DateTimeHelper.CombineDateAndTime(flight.ArrivalDate, flight.ArrivalTime);

                var flightStatus = new FlightStatus
                {
                    FlightNumber = flight.FlightNo,
                    From = flight.From,
                    To = flight.To,
                    Aircraft = flight.AircraftType,
                    DepartureDateTime = departureDateTime,
                    ArrivalDateTime = arrivalDateTime
                };

                flightStatuses.Add(flightStatus);
            }

            return flightStatuses;
        }

        private FlightSeatInventory GetFlightSeatInventory(string flightNumber, string departureDate)
        {
            var key = $"{flightNumber}-{departureDate}";

            if (!_inventoryRepository.SeatInventory.ContainsKey(key))
            {
                var flight = _inventoryRepository.Inventory.First(f =>
                    f.FlightNo == flightNumber && f.DepartureDate == departureDate);

                _inventoryRepository.SeatInventory[key] = new FlightSeatInventory
                {
                    FlightNumber = flightNumber,
                    DepartureDate = departureDate,
                    AircraftType = flight.AircraftType,
                    OccupiedSeats = new HashSet<string>()
                };
            }

            return _inventoryRepository.SeatInventory[key];
        }

        public async Task<bool> AssignSeat(string flightNumber, string departureDate, string seatNumber)
        {
            if (!await IsSeatAvailable(flightNumber, departureDate, seatNumber))
                return false;

            var key = $"{flightNumber}-{departureDate}";
            var inventory = GetFlightSeatInventory(flightNumber, departureDate);

            return inventory.OccupiedSeats.Add(seatNumber);
        }

        public async Task<bool> ReleaseSeat(string flightNumber, string departureDate, string seatNumber)
        {
            var key = $"{flightNumber}-{departureDate}";
            var inventory = GetFlightSeatInventory(flightNumber, departureDate);

            return inventory.OccupiedSeats.Remove(seatNumber);
        }

        public async Task<bool> IsSeatAvailable(string flightNumber, string departureDate, string seatNumber)
        {
            // First check if it's a valid seat
            if (!await IsValidSeat(flightNumber, departureDate, seatNumber))
                return false;

            var key = $"{flightNumber}-{departureDate}";
            var inventory = GetFlightSeatInventory(flightNumber, departureDate);

            return !inventory.OccupiedSeats.Contains(seatNumber);
        }
    }
}
