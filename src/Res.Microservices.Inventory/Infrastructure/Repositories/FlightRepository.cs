using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Res.Microservices.Inventory.Application.DTOs;
using Res.Microservices.Inventory.Domain.Entities;
using Res.Microservices.Inventory.Infrastructure.Data;
using Res.Microservices.Inventory.Infrastructure.Models;

namespace Res.Microservices.Inventory.Infrastructure.Repositories
{
    public class FlightRepository : IFlightRepository
    {
        private readonly InventoryDbContext _context;
        private readonly ILogger<FlightRepository> _logger;

        public FlightRepository(InventoryDbContext context, ILogger<FlightRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Flight>> SearchFlights(DateTime date, string origin, string destination)
        {
            try
            {
                return await _context.Flights
                    .Where(f => f.Departure.Date == date.Date &&
                                f.From == origin &&
                                f.To == destination)
                    .OrderBy(f => f.Departure)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching flights for {Date} {Origin}-{Destination}",
                    date, origin, destination);
                throw;
            }
        }

        public async Task<Flight> GetFlight(string flightNo, DateTime departureDate)
        {
            try
            {
                return await _context.Flights
                    .FirstOrDefaultAsync(f => f.FlightNo == flightNo &&
                                              f.Departure.Date == departureDate.Date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving flight {FlightNo} for {Date}",
                    flightNo, departureDate);
                throw;
            }
        }

        public async Task ImportFlights(List<Flight> flights)
        {
            if (flights == null || !flights.Any())
            {
                throw new ArgumentException("Flight list cannot be null or empty");
            }

            try
            {
                // Create DataTable with matching structure
                var flightTable = new DataTable();
                flightTable.Columns.Add("Reference", typeof(Guid));
                flightTable.Columns.Add("FlightNo", typeof(string));
                flightTable.Columns.Add("From", typeof(string));
                flightTable.Columns.Add("To", typeof(string));
                flightTable.Columns.Add("Departure", typeof(DateTime));
                flightTable.Columns.Add("Arrival", typeof(DateTime));
                flightTable.Columns.Add("Seats", typeof(string));
                flightTable.Columns.Add("AircraftType", typeof(string));
                flightTable.Columns.Add("CreatedDate", typeof(DateTime));
                flightTable.Columns.Add("ModifiedDate", typeof(DateTime));

                // Populate the DataTable
                foreach (var flight in flights)
                {
                    var now = DateTime.UtcNow;
                    flightTable.Rows.Add(
                        flight.Reference == Guid.Empty ? Guid.NewGuid() : flight.Reference,
                        flight.FlightNo,
                        flight.From,
                        flight.To,
                        flight.Departure,
                        flight.Arrival,
                        JsonSerializer.Serialize(flight.Seats),
                        flight.AircraftType,
                        now,
                        now
                    );
                }

                // Get the connection string from DbContext
                var connection = _context.Database.GetDbConnection() as SqlConnection;
                if (connection == null)
                {
                    throw new InvalidOperationException("Database connection is not SQL Server");
                }

                // Ensure connection is open
                if (connection.State != ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }

                // Begin transaction if not already in one
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Configure bulk copy
                    using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
                    {
                        DestinationTableName = "[Inv].[Flight]",
                        BatchSize = 1000,
                        BulkCopyTimeout = 300 // 5 minutes
                    };

                    // Map columns
                    bulkCopy.ColumnMappings.Add("Reference", "Reference");
                    bulkCopy.ColumnMappings.Add("FlightNo", "FlightNo");
                    bulkCopy.ColumnMappings.Add("From", "From");
                    bulkCopy.ColumnMappings.Add("To", "To");
                    bulkCopy.ColumnMappings.Add("Departure", "Departure");
                    bulkCopy.ColumnMappings.Add("Arrival", "Arrival");
                    bulkCopy.ColumnMappings.Add("Seats", "Seats");
                    bulkCopy.ColumnMappings.Add("AircraftType", "AircraftType");
                    bulkCopy.ColumnMappings.Add("CreatedDate", "CreatedDate");
                    bulkCopy.ColumnMappings.Add("ModifiedDate", "ModifiedDate");

                    // Perform bulk insert
                    await bulkCopy.WriteToServerAsync(flightTable);

                    // Commit transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully imported {Count} flights", flights.Count);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk importing {Count} flights", flights.Count);
                throw new Exception("Failed to import flights", ex);
            }
        }

        public async Task<bool> AllocateSeat(SeatAllocationRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var flight = await GetFlight(request.FlightNo, request.DepartureDate);
                if (flight == null)
                {
                    _logger.LogWarning("Flight not found for allocation: {FlightNo} {Date}",
                        request.FlightNo, request.DepartureDate);
                    return false;
                }

                // Get current allocations
                var allocation = await _context.Allocations
                    .FirstOrDefaultAsync(a => a.InventoryReference == flight.Reference);

                // Create new seat allocation
                var newSeatAllocation = new SeatAllocation
                {
                    Seat = request.SeatNumber,
                    RecordLocator = request.RecordLocator,
                    PaxId = request.PaxId
                };

                if (allocation == null)
                {
                    // Create new allocation record
                    allocation = new Allocation
                    {
                        InventoryReference = flight.Reference,
                        Seats = JsonSerializer.Serialize(new[] { newSeatAllocation })
                    };
                    _context.Allocations.Add(allocation);
                }
                else
                {
                    // Update existing allocation
                    var currentAllocations = JsonSerializer.Deserialize<List<SeatAllocation>>(allocation.Seats);

                    // Check if seat is already allocated
                    if (currentAllocations.Any(a => a.Seat == request.SeatNumber))
                    {
                        _logger.LogWarning("Seat {Seat} already allocated on flight {FlightNo}",
                            request.SeatNumber, request.FlightNo);
                        return false;
                    }

                    currentAllocations.Add(newSeatAllocation);
                    allocation.Seats = JsonSerializer.Serialize(currentAllocations);
                    _context.Allocations.Update(allocation);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error allocating seat {Seat} on {FlightNo} for {RecordLocator}",
                    request.SeatNumber, request.FlightNo, request.RecordLocator);
                throw;
            }
        }

        public async Task<List<AllocationResponse>> GetSeatMap(string flightNo, DateTime departureDate)
        {
            try
            {
                // Get flight details
                var flight = await _context.Flights
                    .FirstOrDefaultAsync(f => f.FlightNo == flightNo &&
                                              f.Departure.Date == departureDate.Date);
                if (flight == null) return new List<AllocationResponse>();

                // Get aircraft configuration
                var aircraftConfig = await _context.AircraftConfigs
                    .FirstOrDefaultAsync(ac => ac.AircraftType == flight.AircraftType);
                if (aircraftConfig == null) return new List<AllocationResponse>();

                // Get current allocations
                var allocation = await _context.Allocations
                    .FirstOrDefaultAsync(a => a.InventoryReference == flight.Reference);

                // Parse configuration and allocations
                //var config = JsonSerializer.Deserialize<AircraftConfig>(aircraftConfig.Cabins);
                //var allocations = allocation != null
                //    ? JsonSerializer.Deserialize<List<SeatAllocation>>(allocation.Seats)
                //    : new List<SeatAllocation>();

                var seatMap = new List<AllocationResponse>();

                //// Build complete seat map
                //foreach (var cabin in config.Cabins)
                //{
                //    var cabinInfo = cabin.Value;
                //    for (int row = cabinInfo.FirstRow; row <= cabinInfo.LastRow; row++)
                //    {
                //        foreach (var letter in cabinInfo.SeatLetters)
                //        {
                //            var seatNumber = $"{row}{letter}";
                //            var allocatedSeat = allocations?.FirstOrDefault(a => a.Seat == seatNumber);

                //            seatMap.Add(new AllocationResponse
                //            {
                //                Number = seatNumber,
                //                IsAvailable = allocatedSeat == null &&
                //                              !cabinInfo.BlockedSeats.Any(b => b.SeatNumber == seatNumber),
                //                RecordLocator = allocatedSeat?.RecordLocator,
                //                PaxId = allocatedSeat?.PaxId
                //            });
                //        }
                //    }
                //}

                return seatMap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving seat map for flight {FlightNo} on {Date}",
                    flightNo, departureDate);
                throw;
            }
        }

        public async Task ImportAircraftConfig(List<AircraftConfig> aircraftConfigs)
        {
            try
            {
                // Validate input
                if (aircraftConfigs == null || !aircraftConfigs.Any())
                {
                    throw new ArgumentException("Aircraft configurations list cannot be null or empty");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Get existing configurations to determine what needs to be updated vs inserted
                    var existingConfigs = await _context.AircraftConfigs.ToDictionaryAsync(c => c.AircraftType);

                    foreach (var config in aircraftConfigs)
                    {
                        // Ensure required fields are present
                        if (string.IsNullOrEmpty(config.AircraftType))
                        {
                            _logger.LogWarning("Skipping aircraft config with missing aircraft type");
                            continue;
                        }

                        if (config.Cabins == null || !config.Cabins.Any())
                        {
                            _logger.LogWarning("Skipping aircraft config {Type} with no cabin configuration", config.AircraftType);
                            continue;
                        }

                        // Check if config already exists
                        if (existingConfigs.TryGetValue(config.AircraftType, out var existingConfig))
                        {
                            // Update existing config
                            existingConfig.Cabins = config.Cabins;
                            _context.AircraftConfigs.Update(existingConfig);

                            _logger.LogInformation("Updating existing aircraft config for {Type}", config.AircraftType);
                        }
                        else
                        {
                            // Create new config
                            var newConfig = new AircraftConfig
                            {
                                Reference = Guid.NewGuid(),
                                AircraftType = config.AircraftType,
                                Cabins = config.Cabins,
                            };

                            _context.AircraftConfigs.Add(newConfig);

                            _logger.LogInformation("Adding new aircraft config for {Type}", config.AircraftType);
                        }
                    }

                    // Save all changes in a single transaction
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully imported {Count} aircraft configurations", aircraftConfigs.Count);
                }
                catch (Exception)
                {
                    // If anything fails, roll back the entire transaction
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing aircraft configurations");
                throw;
            }
        }

        public async Task<int> DeleteOldFlights(DateTime cutoffDate)
        {
            try
            {
                // Start a transaction
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // First, delete associated allocations
                    var deletedAllocations = await _context.Allocations
                        .Where(a => a.Flight.Departure < cutoffDate)
                        .ExecuteDeleteAsync();

                    _logger.LogInformation("Deleted {count} old allocation records", deletedAllocations);

                    // Then delete the flights
                    var deletedFlights = await _context.Flights
                        .Where(f => f.Departure < cutoffDate)
                        .ExecuteDeleteAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Deleted {count} old flight records", deletedFlights);

                    return deletedFlights;
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old flights before {cutoffDate}", cutoffDate);
                throw;
            }
        }
    }
}