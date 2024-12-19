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

        private readonly Dictionary<string, Dictionary<string, int>> _aircraftConfigs = new()
        {
            {
                "339", new Dictionary<string, int> {
                    { "J", 32 }, // Business
                    { "W", 46 }, // Premium Economy
                    { "Y", 184 } // Economy
                }
            },
            {
                "351", new Dictionary<string, int> {
                    { "J", 44 }, // Business
                    { "W", 56 }, // Premium Economy
                    { "Y", 235 } // Economy
                }
            },
            {
                "333", new Dictionary<string, int> {
                    { "J", 31 }, // Business
                    { "W", 48 }, // Premium Economy
                    { "Y", 185 } // Economy
                }
            },
            {
                "789", new Dictionary<string, int> {
                    { "J", 31 }, // Business
                    { "W", 35 }, // Premium Economy
                    { "Y", 198 } // Economy
                }
            }
        };

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
            // Create users
            _userRepository.Users.Add(new User { UserId = "SH", Password = "123" });
            _userRepository.Users.Add(new User { UserId = "KP", Password = "123" });

            // Add main VS routes with multiple frequencies
            List<(string from, string to, string[] times)> vsRoutes = new()
            {
                // London - New York (6 daily)
                ("LHR", "JFK", new[] { "0900", "1100", "1300", "1500", "1700", "1900" }),
                ("JFK", "LHR", new[] { "1830", "2030", "2230", "0030", "0230", "0430" }),

                // London - Los Angeles (4 daily)
                ("LHR", "LAX", new[] { "1000", "1400", "1600", "2000" }),
                ("LAX", "LHR", new[] { "1530", "1930", "2130", "2330" }),

                // London - Miami (3 daily)
                ("LHR", "MIA", new[] { "0930", "1330", "1730" }),
                ("MIA", "LHR", new[] { "1630", "2030", "2330" }),

                // London - Boston (3 daily)
                ("LHR", "BOS", new[] { "0830", "1230", "1630" }),
                ("BOS", "LHR", new[] { "1530", "1930", "2230" }),

                // London - San Francisco (3 daily)
                ("LHR", "SFO", new[] { "1030", "1430", "1830" }),
                ("SFO", "LHR", new[] { "1630", "2030", "2330" }),

                // London - Washington (3 daily)
                ("LHR", "IAD", new[] { "1130", "1530", "1930" }),
                ("IAD", "LHR", new[] { "1730", "2130", "0030" }),

                // London - Atlanta (2 daily)
                ("LHR", "ATL", new[] { "1030", "1630" }),
                ("ATL", "LHR", new[] { "1730", "2330" }),

                // London - Las Vegas (2 daily)
                ("LHR", "LAS", new[] { "1130", "1730" }),
                ("LAS", "LHR", new[] { "1830", "0030" }),

                // London - Orlando (2 daily)
                ("LHR", "MCO", new[] { "1030", "1530" }),
                ("MCO", "LHR", new[] { "1730", "2230" }),

                // London - Delhi (2 daily)
                ("LHR", "DEL", new[] { "2130", "2330" }),
                ("DEL", "LHR", new[] { "0430", "0630" }),

                // London - Mumbai (2 daily)
                ("LHR", "BOM", new[] { "2230", "0030" }),
                ("BOM", "LHR", new[] { "0530", "0730" }),

                // London - Shanghai (1 daily)
                ("LHR", "PVG", new[] { "2230" }),
                ("PVG", "LHR", new[] { "0530" }),

                // London - Hong Kong (2 daily)
                ("LHR", "HKG", new[] { "2130", "2330" }),
                ("HKG", "LHR", new[] { "0430", "0630" }),

                // London - Johannesburg (2 daily)
                ("LHR", "JNB", new[] { "2130", "2330" }),
                ("JNB", "LHR", new[] { "0430", "0630" }),

                // Manchester - Orlando (2 daily)
                ("MAN", "MCO", new[] { "1030", "1430" }),
                ("MCO", "MAN", new[] { "1730", "2130" }),

                // Manchester - Atlanta (1 daily)
                ("MAN", "ATL", new[] { "1130" }),
                ("ATL", "MAN", new[] { "1830" })
            };

            Dictionary<(string from, string to), string> routeAircraft = new()
            {
                // Trans-Atlantic routes
                { ("LHR", "JFK"), "789" }, // Boeing 787-9
                { ("JFK", "LHR"), "789" },
                { ("LHR", "LAX"), "351" }, // Airbus A350-900
                { ("LAX", "LHR"), "351" },
                { ("LHR", "MIA"), "333" }, // Airbus A330-300
                { ("MIA", "LHR"), "333" },
                { ("LHR", "BOS"), "333" },
                { ("BOS", "LHR"), "333" },
                { ("LHR", "SFO"), "789" },
                { ("SFO", "LHR"), "789" },
                { ("LHR", "IAD"), "351" },
                { ("IAD", "LHR"), "351" },

                // Asia routes
                { ("LHR", "DEL"), "351" },
                { ("DEL", "LHR"), "351" },
                { ("LHR", "BOM"), "789" },
                { ("BOM", "LHR"), "789" },
                { ("LHR", "PVG"), "351" },
                { ("PVG", "LHR"), "351" },
                { ("LHR", "HKG"), "351" },
                { ("HKG", "LHR"), "351" },

                // Other long-haul
                { ("LHR", "JNB"), "789" },
                { ("JNB", "LHR"), "789" },

                // Default routes use 333
                { ("LHR", "ATL"), "333" },
                { ("ATL", "LHR"), "333" },
                { ("LHR", "LAS"), "333" },
                { ("LAS", "LHR"), "333" },
                { ("LHR", "MCO"), "333" },
                { ("MCO", "LHR"), "333" },
                { ("MAN", "MCO"), "333" },
                { ("MCO", "MAN"), "333" },
                { ("MAN", "ATL"), "333" },
                { ("ATL", "MAN"), "333" }
            };

            for (int day = 0; day < 331; day++)
            {
                int flightNumberCounter = 1; // Start after VS010

                foreach (var route in vsRoutes)
                {
                    foreach (var departureTime in route.times)
                    {
                        // Calculate arrival time and date based on route duration and departure time
                        TimeSpan departureTimeSpan = TimeSpan.ParseExact(departureTime, "hhmm", CultureInfo.InvariantCulture);

                        int durationHours = (route.from, route.to) switch
                        {
                            ("LHR", "JFK") or ("JFK", "LHR") => 7,
                            ("LHR", "LAX") or ("LAX", "LHR") => 11,
                            ("LHR", "MIA") or ("MIA", "LHR") => 9,
                            ("LHR", "BOS") or ("BOS", "LHR") => 7,
                            ("LHR", "SFO") or ("SFO", "LHR") => 11,
                            ("LHR", "IAD") or ("IAD", "LHR") => 8,
                            ("LHR", "ATL") or ("ATL", "LHR") => 9,
                            ("LHR", "LAS") or ("LAS", "LHR") => 11,
                            ("LHR", "MCO") or ("MCO", "LHR") => 9,
                            ("LHR", "DEL") or ("DEL", "LHR") => 8,
                            ("LHR", "BOM") or ("BOM", "LHR") => 9,
                            ("LHR", "PVG") or ("PVG", "LHR") => 11,
                            ("LHR", "HKG") or ("HKG", "LHR") => 12,
                            ("LHR", "JNB") or ("JNB", "LHR") => 11,
                            ("MAN", "MCO") or ("MCO", "MAN") => 9,
                            ("MAN", "ATL") or ("ATL", "MAN") => 9,
                            _ => 8 // Default duration
                        };

                        DateTime baseDate = DateTime.Today.AddDays(day);
                        DateTime departureDateTime = baseDate.Add(departureTimeSpan);
                        DateTime arrivalDateTime = departureDateTime.AddHours(durationHours);

                        string arrivalDate = arrivalDateTime.ToString("ddMMM").ToUpper();
                        string arrivalTime = arrivalDateTime.ToString("HHmm");

                        // Create flight number
                        string flightNo = $"VS{flightNumberCounter:D03}";

                        // Get the aircraft type for this route
                        string aircraftType = routeAircraft.TryGetValue((route.from, route.to), out string aircraft) ? aircraft : "333";

                        // Get the seat configuration for this aircraft type
                        var seatConfig = _aircraftConfigs[aircraftType];

                        _newFlights.Add(new FlightInventory
                        {
                            FlightNo = flightNo,
                            From = route.from,
                            To = route.to,
                            DepartureDate = baseDate.ToString("ddMMM").ToUpper(),
                            ArrivalDate = arrivalDate,
                            DepartureTime = departureTime,
                            ArrivalTime = arrivalTime,
                            AircraftType = aircraftType,
                            Seats = new Dictionary<string, int>(seatConfig) // Use the correct seat configuration
                        });

                        flightNumberCounter++;
                    }
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

            // Initialize seat configurations
            _seatMapRepository.SeatConfigurations = InitializeSeatConfigurations();

            if (!includePnrs)
                return;

            // login 
            var loginResponse = _userService.Authenticate("SH", "123");

            _token = loginResponse.Token;

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
                FindFlightAndGenerateLongSellCommand("VS001",3, "J"), // Select the next VS001 flight (specific flight)
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
                "CKIN {PNR}/P1/VS001",
                "CKIN {PNR}/P2/VS001",
                "IG",
            });

            CreatePnr(_reservationSystem, new List<string>
            {
                "NM1HARVEY/SEB MR",
                "NM1HARVEY/CLAIRE MRS",
                FindFlightAndGenerateLongSellCommand("VS001",3, "J"), // Select the next VS001 flight (specific flight)
                FindFlightAndGenerateLongSellCommand(3, "J", "JFK", "LHR", DateTime.Today.AddDays(7).ToString("ddMMM").ToUpper()),
                "CTCP 0727777777",
                "TLTL08MAY",
                "FXP",
                "FS",
                "FP*CC/VISA/4166676667666746/0625/GBP892.00",
                "ER",
                "TTP",
                "IG",
            });
        }

        public Dictionary<string, SeatConfiguration> InitializeSeatConfigurations()
        {
            var configs = new Dictionary<string, SeatConfiguration>();

            // A350-900 (351) Configuration
            configs.Add("351", new SeatConfiguration
            {
                AircraftType = "351",
                Cabins = new Dictionary<string, CabinConfiguration>
                {
                    {
                        "J", new CabinConfiguration // Business Class
                        {
                            CabinCode = "J",
                            CabinName = "Upper Class",
                            FirstRow = 1,
                            LastRow = 11,
                            SeatLetters = new List<string> { "A", "D", "G", "K" },
                            BulkheadRows = new List<int> { 1 },
                            GalleryRows = new List<int> { 1, 11 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    },
                    {
                        "W", new CabinConfiguration // Premium Economy
                        {
                            CabinCode = "W",
                            CabinName = "Premium",
                            FirstRow = 20,
                            LastRow = 27,
                            SeatLetters = new List<string> { "A", "C", "D", "E", "G", "H", "K" },
                            BulkheadRows = new List<int> { 20 },
                            ExitRows = new List<int> { 20 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "C", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "E", new BaseSeatDefinition { IsWindow = false, IsAisle = false, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "H", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Right" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    },
                    {
                        "Y", new CabinConfiguration // Economy
                        {
                            CabinCode = "Y",
                            CabinName = "Economy",
                            FirstRow = 30,
                            LastRow = 63,
                            SeatLetters = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K" },
                            BulkheadRows = new List<int> { 30 },
                            ExitRows = new List<int> { 30, 46 },
                            GalleryRows = new List<int> { 46 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "B", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Left" } },
                                { "C", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "E", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Center" } },
                                { "F", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "H", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Right" } },
                                { "J", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Right" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    }
                }
            });

            // Boeing 787-9 (789) Configuration
            configs.Add("789", new SeatConfiguration
            {
                AircraftType = "789",
                Cabins = new Dictionary<string, CabinConfiguration>
                {
                    {
                        "J", new CabinConfiguration
                        {
                            CabinCode = "J",
                            CabinName = "Upper Class",
                            FirstRow = 1,
                            LastRow = 8,
                            SeatLetters = new List<string> { "A", "D", "G", "K" },
                            BulkheadRows = new List<int> { 1 },
                            GalleryRows = new List<int> { 1, 8 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    },
                    {
                        "W", new CabinConfiguration
                        {
                            CabinCode = "W",
                            CabinName = "Premium",
                            FirstRow = 20,
                            LastRow = 24,
                            SeatLetters = new List<string> { "A", "C", "D", "E", "G", "H", "K" },
                            BulkheadRows = new List<int> { 20 },
                            ExitRows = new List<int> { 20 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "C", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "E", new BaseSeatDefinition { IsWindow = false, IsAisle = false, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "H", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Right" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    },
                    {
                        "Y", new CabinConfiguration
                        {
                            CabinCode = "Y",
                            CabinName = "Economy",
                            FirstRow = 30,
                            LastRow = 58,
                            SeatLetters = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K" },
                            BulkheadRows = new List<int> { 30 },
                            ExitRows = new List<int> { 30, 44 },
                            GalleryRows = new List<int> { 44 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "B", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Left" } },
                                { "C", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "E", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Center" } },
                                { "F", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "H", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Right" } },
                                { "J", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Right" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    }
                }
            });

            // A330-300 (333) Configuration
            configs.Add("333", new SeatConfiguration
            {
                AircraftType = "333",
                Cabins = new Dictionary<string, CabinConfiguration>
                {
                    {
                        "J", new CabinConfiguration
                        {
                            CabinCode = "J",
                            CabinName = "Upper Class",
                            FirstRow = 1,
                            LastRow = 9,
                            SeatLetters = new List<string> { "A", "D", "G", "K" },
                            BulkheadRows = new List<int> { 1 },
                            GalleryRows = new List<int> { 1, 9 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    },
                    {
                        "W", new CabinConfiguration
                        {
                            CabinCode = "W",
                            CabinName = "Premium",
                            FirstRow = 20,
                            LastRow = 26,
                            SeatLetters = new List<string> { "A", "C", "D", "E", "G", "H", "K" },
                            BulkheadRows = new List<int> { 20 },
                            ExitRows = new List<int> { 20 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "C", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "E", new BaseSeatDefinition { IsWindow = false, IsAisle = false, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "H", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Right" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    },
                    {
                        "Y", new CabinConfiguration
                        {
                            CabinCode = "Y",
                            CabinName = "Economy",
                            FirstRow = 30,
                            LastRow = 60,
                            SeatLetters = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K" },
                            BulkheadRows = new List<int> { 30 },
                            ExitRows = new List<int> { 30, 45 },
                            GalleryRows = new List<int> { 45 },
                            BlockedSeats = new List<BlockedSeat>
                            {
                                new BlockedSeat { SeatNumber = "31D", Reason = "Crew Rest" },
                                new BlockedSeat { SeatNumber = "31G", Reason = "Crew Rest" }
                            },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "B", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Left" } },
                                { "C", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "E", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Center" } },
                                { "F", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "H", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Right" } },
                                { "J", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Right" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    }
                }
            });

            // A330-900neo (339) Configuration
            configs.Add("339", new SeatConfiguration
            {
                AircraftType = "339",
                Cabins = new Dictionary<string, CabinConfiguration>
                {
                    {
                        "J", new CabinConfiguration
                        {
                            CabinCode = "J",
                            CabinName = "Upper Class",
                            FirstRow = 1,
                            LastRow = 8,
                            SeatLetters = new List<string> { "A", "D", "G", "K" },
                            BulkheadRows = new List<int> { 1 },
                            GalleryRows = new List<int> { 1, 8 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    },
                    {
                        "W", new CabinConfiguration
                        {
                            CabinCode = "W",
                            CabinName = "Premium",
                            FirstRow = 20,
                            LastRow = 25,
                            SeatLetters = new List<string> { "A", "C", "D", "E", "G", "H", "K" },
                            BulkheadRows = new List<int> { 20 },
                            ExitRows = new List<int> { 20 },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "C", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "E", new BaseSeatDefinition { IsWindow = false, IsAisle = false, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "H", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Right" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    },
                    {
                        "Y", new CabinConfiguration
                        {
                            CabinCode = "Y",
                            CabinName = "Economy",
                            FirstRow = 30,
                            LastRow = 61,
                            SeatLetters = new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K" },
                            BulkheadRows = new List<int> { 30 },
                            ExitRows = new List<int> { 30, 46 },
                            GalleryRows = new List<int> { 46 },
                            BlockedSeats = new List<BlockedSeat>
                            {
                                new BlockedSeat { SeatNumber = "31D", Reason = "Crew Rest" },
                                new BlockedSeat { SeatNumber = "31G", Reason = "Crew Rest" },
                                new BlockedSeat { SeatNumber = "46D", Reason = "Equipment" },
                                new BlockedSeat { SeatNumber = "46G", Reason = "Equipment" }
                            },
                            SeatDefinitions = new Dictionary<string, BaseSeatDefinition>
                            {
                                { "A", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Left" } },
                                { "B", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Left" } },
                                { "C", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Left" } },
                                { "D", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "E", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Center" } },
                                { "F", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Center" } },
                                { "G", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Center" } },
                                { "H", new BaseSeatDefinition { IsWindow = false, IsAisle = true, Position = "Right" } },
                                { "J", new BaseSeatDefinition { IsWindow = false, IsMiddle = true, Position = "Right" } },
                                { "K", new BaseSeatDefinition { IsWindow = true, IsAisle = false, Position = "Right" } }
                            }
                        }
                    }
                }
            });

            return configs;
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