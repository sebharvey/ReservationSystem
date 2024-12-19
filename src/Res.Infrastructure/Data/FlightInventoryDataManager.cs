using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Res.Domain.Entities.Inventory;

namespace Res.Infrastructure.Data
{
    public class FlightInventoryDataManager : IFlightInventoryDataManager
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public FlightInventoryDataManager(string connectionString)
        {
            _connectionString = connectionString;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        public async Task<bool> CreateFlightAsync(FlightInventory flight)
        {
            const string sql = @"
            INSERT INTO FlightInventory (
                FlightNo, 
                Origin, 
                Destination, 
                DepartureDate,
                ArrivalDate,
                Seats,
                DepartureTime,
                ArrivalTime,
                AircraftType
            )
            VALUES (
                @FlightNo,
                @Origin,
                @Destination,
                @DepartureDate,
                @ArrivalDate,
                @Seats,
                @DepartureTime,
                @ArrivalTime,
                @AircraftType
            )";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FlightNo", flight.FlightNo);
            command.Parameters.AddWithValue("@Origin", flight.From);
            command.Parameters.AddWithValue("@Destination", flight.To);
            command.Parameters.AddWithValue("@DepartureDate", flight.DepartureDate);
            command.Parameters.AddWithValue("@ArrivalDate", flight.ArrivalDate);
            command.Parameters.AddWithValue("@Seats", JsonSerializer.Serialize(flight.Seats));
            command.Parameters.AddWithValue("@DepartureTime", flight.DepartureTime);
            command.Parameters.AddWithValue("@ArrivalTime", flight.ArrivalTime);
            command.Parameters.AddWithValue("@AircraftType", flight.AircraftType);

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error creating flight {flight.FlightNo}", ex);
            }
        }

        public async Task<FlightInventory> GetFlightAsync(string flightNo, string departureDate)
        {
            const string sql = @"
            SELECT 
                FlightNo,
                Origin as [From],
                Destination as [To],
                DepartureDate,
                ArrivalDate,
                Seats,
                DepartureTime,
                ArrivalTime,
                AircraftType
            FROM FlightInventory
            WHERE FlightNo = @FlightNo 
            AND DepartureDate = @DepartureDate";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FlightNo", flightNo);
            command.Parameters.AddWithValue("@DepartureDate", departureDate);

            try
            {
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new FlightInventory
                    {
                        FlightNo = reader.GetString(reader.GetOrdinal("FlightNo")),
                        From = reader.GetString(reader.GetOrdinal("From")),
                        To = reader.GetString(reader.GetOrdinal("To")),
                        DepartureDate = reader.GetString(reader.GetOrdinal("DepartureDate")),
                        ArrivalDate = reader.GetString(reader.GetOrdinal("ArrivalDate")),
                        Seats = JsonSerializer.Deserialize<Dictionary<string, int>>(
                            reader.GetString(reader.GetOrdinal("Seats")),
                            _jsonOptions),
                        DepartureTime = reader.GetString(reader.GetOrdinal("DepartureTime")),
                        ArrivalTime = reader.GetString(reader.GetOrdinal("ArrivalTime")),
                        AircraftType = reader.GetString(reader.GetOrdinal("AircraftType"))
                    };
                }

                return null;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error retrieving flight {flightNo}", ex);
            }
        }

        public Task<bool> UpdateFlightAsync(FlightInventory flight)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteFlightAsync(string flightNo, string departureDate)
        {
            throw new NotImplementedException();
        }

        public async Task<List<FlightInventory>> SearchAvailabilityAsync(string from, string to, string departureDate, string preferredTime = null)
        {
            var sql = @"
            SELECT 
                FlightNo,
                Origin as [From],
                Destination as [To],
                DepartureDate,
                ArrivalDate,
                Seats,
                DepartureTime,
                ArrivalTime,
                AircraftType
            FROM FlightInventory
            WHERE Origin = @From 
            AND Destination = @To 
            AND DepartureDate = @DepartureDate";

            if (!string.IsNullOrEmpty(preferredTime))
            {
                sql += " AND DepartureTime = @PreferredTime";
            }

            sql += " ORDER BY DepartureTime";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@From", from);
            command.Parameters.AddWithValue("@To", to);
            command.Parameters.AddWithValue("@DepartureDate", departureDate);

            if (!string.IsNullOrEmpty(preferredTime))
            {
                command.Parameters.AddWithValue("@PreferredTime", preferredTime);
            }

            var flights = new List<FlightInventory>();

            try
            {
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    flights.Add(new FlightInventory
                    {
                        FlightNo = reader.GetString(reader.GetOrdinal("FlightNo")),
                        From = reader.GetString(reader.GetOrdinal("From")),
                        To = reader.GetString(reader.GetOrdinal("To")),
                        DepartureDate = reader.GetString(reader.GetOrdinal("DepartureDate")),
                        ArrivalDate = reader.GetString(reader.GetOrdinal("ArrivalDate")),
                        Seats = JsonSerializer.Deserialize<Dictionary<string, int>>(
                            reader.GetString(reader.GetOrdinal("Seats")),
                            _jsonOptions),
                        DepartureTime = reader.GetString(reader.GetOrdinal("DepartureTime")),
                        ArrivalTime = reader.GetString(reader.GetOrdinal("ArrivalTime")),
                        AircraftType = reader.GetString(reader.GetOrdinal("AircraftType"))
                    });
                }

                return flights;
            }
            catch (SqlException ex)
            {
                throw new DataException("Error searching flights", ex);
            }
        }

        public Task<bool> UpdateInventoryLevelsAsync(string flightNo, string departureDate, Dictionary<string, int> newLevels)
        {
            throw new NotImplementedException();
        }

        // Additional methods implementation...
    }
}