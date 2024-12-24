using Res.Application.ReservationSystem;
using Res.Core.Interfaces;
using System.Diagnostics;
using System.Globalization;
using Res.Domain.Entities.Inventory;
using Res.Domain.Entities.User;
using Res.Infrastructure.Interfaces;

namespace Res.Tests.Data
{
    public class TestData
    {
        private readonly IInventoryService _inventoryService;
        private readonly IReservationSystem _reservationSystem;
        private readonly ISeatMapRepository _seatMapRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUserService _userService;

        private readonly List<FlightInventory> _newFlights = new();

        private string _token;

        public TestData(IInventoryService inventoryService, IReservationSystem reservationSystem, ISeatMapRepository seatMapRepository, IUserRepository userRepository, IUserService userService)
        {
            _inventoryService = inventoryService;
            _reservationSystem = reservationSystem;
            _seatMapRepository = seatMapRepository;
            _userRepository = userRepository;
            _userService = userService;
        }

        public void Initialize(bool includePnrs = true)
        {
            try
            {
                // Create users
                _userRepository.Users.Add(new User { UserId = "SH", Password = "123" });
                _userRepository.Users.Add(new User { UserId = "KP", Password = "123" });

                // Load flight data from CSV
                var flightDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "Res.Tests.Data", "Data", "FlightData.csv");
                if (!File.Exists(flightDataPath))
                {
                    throw new FileNotFoundException($"Flight data file not found at: {flightDataPath}");
                }

                var flightData = FlightDataHelper.LoadFlightData(flightDataPath);

                // Load seat configurations
                _seatMapRepository.SeatConfigurations = SeatConfigurationLoader.LoadSeatConfigurations();

                // Calculate date range
                var startDate = DateTime.Today;
                const int daysToGenerate = 331;

                foreach (var flight in flightData)
                {
                    if (!FlightDataHelper.ValidateFrequencyString(flight.Freq))
                    {
                        Debug.WriteLine($"Warning: Invalid frequency string '{flight.Freq}' for flight {flight.FlightNo}");
                        continue;
                    }

                    // Validate aircraft type exists in configuration
                    if (!_seatMapRepository.SeatConfigurations.ContainsKey(flight.AircraftType))
                    {
                        Debug.WriteLine($"Warning: No seat configuration found for aircraft type '{flight.AircraftType}' for flight {flight.FlightNo}");
                        continue;
                    }

                    // Get operating dates based on frequency
                    var operatingDates = FlightDataHelper.GetOperatingDates(flight.Freq, startDate, daysToGenerate);

                    foreach (var operatingDate in operatingDates)
                    {
                        // Calculate arrival date/time
                        var departureDateTime = operatingDate.Date.Add(flight.Dep.ToTimeSpan());
                        var arrivalDateTime = operatingDate.Date.Add(flight.Arr.ToTimeSpan());

                        // If arrival time is before departure time, it must be next day
                        if (arrivalDateTime < departureDateTime)
                        {
                            arrivalDateTime = arrivalDateTime.AddDays(1);
                        }

                        // Get seat capacities from configuration
                        var seatConfig = _seatMapRepository.SeatConfigurations[flight.AircraftType];
                        var seats = new Dictionary<string, int>();
                        foreach (var cabin in seatConfig.Cabins)
                        {
                            var seatCount = CalculateCabinCapacity(cabin.Value);
                            seats[cabin.Key] = seatCount;
                        }

                        _newFlights.Add(new FlightInventory
                        {
                            FlightNo = flight.FlightNo,
                            From = flight.Origin,
                            To = flight.Dest,
                            DepartureDate = departureDateTime.ToString("ddMMM").ToUpper(),
                            ArrivalDate = arrivalDateTime.ToString("ddMMM").ToUpper(),
                            DepartureTime = flight.Dep.ToString("HHmm"),
                            ArrivalTime = flight.Arr.ToString("HHmm"),
                            AircraftType = flight.AircraftType,
                            Seats = seats
                        });
                    }
                }

                // Add the new flights to the existing list
                foreach (var flight in _newFlights)
                {
                    _inventoryService.AddFlight(
                        flight.FlightNo,
                        flight.From,
                        flight.To,
                        flight.DepartureDate,
                        flight.ArrivalDate,
                        flight.DepartureTime,
                        flight.ArrivalTime,
                        flight.Seats,
                        flight.AircraftType);
                }

                Debug.WriteLine($"{_newFlights.Count} test flight records created");

                if (!includePnrs)
                    return;

                // login 
                var loginResponse = _userService.Authenticate("SH", "123");
                _token = loginResponse.Token;

                // Create test PNRs
                CreateTestPnrs();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing test data: {ex.Message}");
                throw;
            }
        }

        private void CreateTestPnrs()
        {
            // Create some PNRs
            CreatePnr(_reservationSystem, new List<string>
            {
                "NM1JONES/PAUL MR",
                "NM2JONES/KELLY MRS",
                "NM3JONES/ROB MAST",
                GenerateLongSellCommand(3, "Y"),
                "CTCP 0777777777",
                "TLTL08MAY",
                "RM FREQUENT FLYER 847587434",
                "ER",
                "RT{PNR}",
                "FXP",
                "FP*CA/GBP892.00",
                "FS",
                "RM NEW REMARK",
                "ER",
                "IG",
            });

            CreatePnr(_reservationSystem, new List<string>
            {
                "NM1HARVEY/JOHN MR",
                "NM2HARVEY/SEB MR",
                "NM3HARVEY/ZACHARIAH MAST",
                FindFlightAndGenerateLongSellCommand(3, "J", "JFK", "LHR", "01MAY"),
                FindFlightAndGenerateLongSellCommand(3, "J", "LHR", "BOM", "03MAY"),
                FindFlightAndGenerateLongSellCommand(3, "J", "BOM", "LHR", "12MAY"),
                FindFlightAndGenerateLongSellCommand(3, "J", "LHR", "JFK", "14MAY"),
                "CTCP 0777777777",
                "TLTL08MAY",
                "ER",
                "RT{PNR}",
                "FXP",
                "FP*MS/INVOICE/GBP892.00",
                "FS",
                "RM NEW REMARK",
                "ER",
                "TTP",
                "IG",
            });

            CreatePnr(_reservationSystem, new List<string>
            {
                "NM1CLARKE/ANDREW MR",
                "NM2CLARKE/JESSICA MRS",
                "NM3TAYLOR/HANNAH MISS",
                "NM4TAYLOR/CHARLOTTE MISS",
                GenerateLongSellCommand(4, "Y"),
                "CTCP 0717777777",
                "TLTL08MAY",
                "ER",
                "IG",
            });

            CreatePnr(_reservationSystem, new List<string>
            {
                "NM1HUTSON/ROBERT MR",
                GenerateLongSellCommand(3, "Y"),
                "CTCP 0727777777",
                "TLTL08MAY",
                "SR WCHR/P1/S1/NEEDS ASSISTANCE FROM GATE",
                "SR VGML/P1",
                "SR SPML/P1/S1/LOW SALT DIET",
                "ER",
                "IG",
            });

            CreatePnr(_reservationSystem, new List<string>
            {
                "NM1DIMITRIOU/DIMITRI MR",
                "NM1DIMITRIOU/LOUISA MRS",
                FindFlightAndGenerateLongSellCommand("VS003",3, "J"), // Select the next VS003 flight (specific flight)
                FindFlightAndGenerateLongSellCommand(3, "J", "JFK", "LHR", DateTime.Today.AddDays(7).ToString("ddMMM").ToUpper()),
                "CTCP 0727777777",
                "TLTL08MAY",
                "SR WCHR/P1/S1/NEEDS ASSISTANCE FROM GATE",
                "SR VGML/P1",
                "SR SPML/P1/S1/LOW SALT DIET",
                "ER",
                "RT{PNR}",
                //"ST/1D/P1/S1",   // Seat allocations
                //"ST/1D/P1/S2",   // Seat allocations
                //"ST/1A/P2/S1",   // Seat allocations
                //"ST/1A/P2/S2",   // Seat allocations
                "FXP",
                "FS",
                "FP*CC/VISA/4444333322221111/0625/GBP892.00",
                "ER",
                "TTP",
                "SRDOCS HK1/P/GBR/P12345678/GBR/12JUL82/M/20NOV25/DIMITRIOU/DIMITRI",    // Add passport doc for first pax  - required for APIS info
                "SRDOCS HK1/P/GBR/P32434338/GBR/01JAN82/M/20NOV25/DIMITRIOU/LOUISA",     // Add passport doc for second pax - required for APIS info
                "SRDOCA HK1/P1/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA",                 // Add address info for first pax  - required for APIS info
                "SRDOCA HK1/P2/R/GBR/123 HIGH STREET/LONDON/GB/W1A 1AA",                 // Add address info for second pax - required for APIS info
                "ER",
                "CKIN {PNR}/P1/VS003",
                "CKIN {PNR}/P2/VS003",
                "ER",
                "IG",
            });

            CreatePnr(_reservationSystem, new List<string>
            {
                "NM1HARVEY/SEB MR",
                "NM1HARVEY/CLAIRE MRS",
                FindFlightAndGenerateLongSellCommand("VS003",3, "J"), // Select the next VS003 flight (specific flight)
                FindFlightAndGenerateLongSellCommand(3, "J", "JFK", "LHR", DateTime.Today.AddDays(7).ToString("ddMMM").ToUpper()),
                "CTCP 0727777777",
                "TLTL08MAY",
                "FXP",
                "FS",
                "FP*CC/VISA/4166676667666746/0625/GBP892.00",
                "ER",
                "TTP",
                "ER",
                "IG",
            });
        }

        private int CalculateCabinCapacity(CabinConfiguration cabin)
        {
            // Calculate total number of seats in the cabin
            var rowCount = cabin.LastRow - cabin.FirstRow + 1;
            var seatsPerRow = cabin.SeatLetters.Count;
            var totalSeats = rowCount * seatsPerRow;

            // Subtract blocked seats if any
            if (cabin.BlockedSeats != null)
            {
                totalSeats -= cabin.BlockedSeats.Count;
            }

            return totalSeats;
        }

        private string FindFlightAndGenerateLongSellCommand(string flightNo, int qty, string cabin)
        {
            var flight = _newFlights
                .Where(flight =>
                {
                    var departureDate = DateTime.ParseExact(flight.DepartureDate, "ddMMM", CultureInfo.InvariantCulture).Date;
                    var departureTime = TimeOnly.ParseExact(flight.DepartureTime, "HHmm", CultureInfo.InvariantCulture);
                    var departureDateTime = departureDate.Add(departureTime.ToTimeSpan());

                    return flight.FlightNo == flightNo && departureDateTime > DateTime.Now;
                })
                .OrderBy(flight => DateTime.ParseExact(flight.DepartureDate, "ddMMM", CultureInfo.InvariantCulture).Date)
                .First();

            return $"SS {flightNo}{cabin}{flight.DepartureDate}{flight.From}{flight.To}{qty}";
        }

        private string FindFlightAndGenerateLongSellCommand(int qty, string cabin, string from, string to, string departureDate, bool nextAvailable = false)
        {
            FlightInventory flight;

            if (nextAvailable)
            {
                flight = _newFlights
                    .First(f => DateTime.ParseExact(f.DepartureDate, "ddMMM", CultureInfo.InvariantCulture).Date > DateTime.Today ||
                                (DateTime.ParseExact(f.DepartureDate, "ddMMM", CultureInfo.InvariantCulture).Date == DateTime.Today &&
                                 TimeOnly.ParseExact(f.DepartureTime, "HHmm", CultureInfo.InvariantCulture) > TimeOnly.FromDateTime(DateTime.Now)));
            }
            else
            {
                flight = _newFlights.First(item => item.From == from && item.To == to && item.DepartureDate == departureDate);
            }

            return $"SS {flight.FlightNo}{cabin}{flight.DepartureDate}{flight.From}{flight.To}{qty}";
        }

        private string GenerateLongSellCommand(int qty, string cabin)
        {
            var flight = _newFlights.MinBy(x => Guid.NewGuid());

            return $"SS {flight.FlightNo}{cabin}{flight.DepartureDate}{flight.From}{flight.To}{qty}";
        }

        private void CreatePnr(IReservationSystem reservationSystem, List<string> commands)
        {
            string pnr = string.Empty;

            foreach (var command in commands)
            {
                var cmd = command.Replace("{PNR}", pnr);

                var output = reservationSystem.ProcessCommand(cmd, _token);

                Debug.WriteLine($"CMD>{cmd}");
                Debug.WriteLine($"{output.Result.Response}");

                if (command == "ER")
                {
                    pnr = output.Result.Response.ToString()!.Substring(5, 6);
                }
            }
        }
    }
}