using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Res.Domain.Entities.Inventory;

namespace Res.Infrastructure.Data
{
    public class FlightSeatInventoryDataManager : IFlightSeatInventoryDataManager
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public FlightSeatInventoryDataManager(string connectionString)
        {
            _connectionString = connectionString;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        public async Task<bool> CreateSeatInventoryAsync(FlightSeatInventory seatInventory)
        {
            const string sql = @"
            INSERT INTO FlightSeatInventory (
                FlightNumber,
                DepartureDate,
                OccupiedSeats,
                AircraftType
            )
            VALUES (
                @FlightNumber,
                @DepartureDate,
                @OccupiedSeats,
                @AircraftType
            )";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FlightNumber", seatInventory.FlightNumber);
            command.Parameters.AddWithValue("@DepartureDate", seatInventory.DepartureDate);
            command.Parameters.AddWithValue("@OccupiedSeats", JsonSerializer.Serialize(seatInventory.OccupiedSeats));
            command.Parameters.AddWithValue("@AircraftType", seatInventory.AircraftType);

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error creating seat inventory for flight {seatInventory.FlightNumber}", ex);
            }
        }

        public Task<FlightSeatInventory> GetSeatInventoryAsync(string flightNumber, string departureDate)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateSeatInventoryAsync(FlightSeatInventory seatInventory)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteSeatInventoryAsync(string flightNumber, string departureDate)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> AssignSeatAsync(string flightNumber, string departureDate, string seatNumber)
        {
            // Start a transaction to ensure atomic operation
            const string sql = @"
            DECLARE @OccupiedSeats nvarchar(max);
            SELECT @OccupiedSeats = OccupiedSeats 
            FROM FlightSeatInventory 
            WHERE FlightNumber = @FlightNumber 
            AND DepartureDate = @DepartureDate;

            IF NOT EXISTS (
                SELECT value 
                FROM OPENJSON(@OccupiedSeats)
                WHERE value = @SeatNumber
            )
            BEGIN
                UPDATE FlightSeatInventory
                SET OccupiedSeats = JSON_MODIFY(
                    OccupiedSeats,
                    'append $',
                    @SeatNumber
                )
                WHERE FlightNumber = @FlightNumber
                AND DepartureDate = @DepartureDate;

                SELECT 1;
            END
            ELSE
                SELECT 0;";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FlightNumber", flightNumber);
            command.Parameters.AddWithValue("@DepartureDate", departureDate);
            command.Parameters.AddWithValue("@SeatNumber", seatNumber);

            try
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error assigning seat {seatNumber}", ex);
            }
        }

        public Task<bool> ReleaseSeatAsync(string flightNumber, string departureDate, string seatNumber)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsSeatAvailableAsync(string flightNumber, string departureDate, string seatNumber)
        {
            const string sql = @"
            SELECT CASE WHEN NOT EXISTS (
                SELECT value 
                FROM FlightSeatInventory
                CROSS APPLY OPENJSON(OccupiedSeats)
                WHERE FlightNumber = @FlightNumber
                AND DepartureDate = @DepartureDate
                AND value = @SeatNumber
            ) THEN 1 ELSE 0 END";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FlightNumber", flightNumber);
            command.Parameters.AddWithValue("@DepartureDate", departureDate);
            command.Parameters.AddWithValue("@SeatNumber", seatNumber);

            try
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error checking seat availability for {seatNumber}", ex);
            }
        }

        // Additional methods implementation...
    }
}