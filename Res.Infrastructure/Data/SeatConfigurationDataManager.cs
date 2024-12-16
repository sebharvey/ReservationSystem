using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Res.Domain.Entities.Inventory;

namespace Res.Infrastructure.Data
{
    public class SeatConfigurationDataManager : ISeatConfigurationDataManager
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public SeatConfigurationDataManager(string connectionString)
        {
            _connectionString = connectionString;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        public async Task<bool> CreateConfigurationAsync(SeatConfiguration config)
        {
            const string sql = @"
            INSERT INTO res.SeatConfiguration (
                AircraftType,
                Cabins,
                CreatedDate,
                LastModifiedDate
            )
            VALUES (
                @AircraftType,
                @Cabins,
                GETUTCDATE(),
                GETUTCDATE()
            )";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@AircraftType", config.AircraftType);
            command.Parameters.AddWithValue("@Cabins", JsonSerializer.Serialize(config.Cabins, _jsonOptions));

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error creating seat configuration for {config.AircraftType}", ex);
            }
        }

        public async Task<SeatConfiguration> GetConfigurationAsync(string aircraftType)
        {
            const string sql = @"
            SELECT 
                AircraftType,
                Cabins
            FROM res.SeatConfiguration
            WHERE AircraftType = @AircraftType";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@AircraftType", aircraftType);

            try
            {
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new SeatConfiguration
                    {
                        AircraftType = reader.GetString(reader.GetOrdinal("AircraftType")),
                        Cabins = JsonSerializer.Deserialize<Dictionary<string, CabinConfiguration>>(
                            reader.GetString(reader.GetOrdinal("Cabins")),
                            _jsonOptions)
                    };
                }

                return null;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error retrieving seat configuration for {aircraftType}", ex);
            }
        }

        public async Task<bool> UpdateConfigurationAsync(SeatConfiguration config)
        {
            const string sql = @"
            UPDATE res.SeatConfiguration
            SET 
                Cabins = @Cabins,
                LastModifiedDate = GETUTCDATE()
            WHERE AircraftType = @AircraftType";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@AircraftType", config.AircraftType);
            command.Parameters.AddWithValue("@Cabins", JsonSerializer.Serialize(config.Cabins, _jsonOptions));

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error updating seat configuration for {config.AircraftType}", ex);
            }
        }

        public async Task<bool> UpdateCabinAsync(string aircraftType, string cabinCode, CabinConfiguration cabinConfig)
        {
            const string sql = @"
            DECLARE @Cabins nvarchar(max);
            SELECT @Cabins = Cabins 
            FROM res.SeatConfiguration 
            WHERE AircraftType = @AircraftType;

            UPDATE res.SeatConfiguration
            SET Cabins = JSON_MODIFY(
                @Cabins,
                @JsonPath,
                @CabinConfig
            ),
            LastModifiedDate = GETUTCDATE()
            WHERE AircraftType = @AircraftType";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@AircraftType", aircraftType);
            command.Parameters.AddWithValue("@JsonPath", $"$.{cabinCode}");
            command.Parameters.AddWithValue("@CabinConfig", JsonSerializer.Serialize(cabinConfig, _jsonOptions));

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error updating cabin configuration for {aircraftType}/{cabinCode}", ex);
            }
        }

        public async Task<List<SeatConfiguration>> GetAllConfigurationsAsync()
        {
            const string sql = @"
            SELECT 
                AircraftType,
                Cabins
            FROM res.SeatConfiguration
            ORDER BY AircraftType";

            var configs = new List<SeatConfiguration>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            try
            {
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    configs.Add(new SeatConfiguration
                    {
                        AircraftType = reader.GetString(reader.GetOrdinal("AircraftType")),
                        Cabins = JsonSerializer.Deserialize<Dictionary<string, CabinConfiguration>>(
                            reader.GetString(reader.GetOrdinal("Cabins")),
                            _jsonOptions)
                    });
                }

                return configs;
            }
            catch (SqlException ex)
            {
                throw new DataException("Error retrieving seat configurations", ex);
            }
        }

        public async Task<bool> DeleteConfigurationAsync(string aircraftType)
        {
            const string sql = @"
            DELETE FROM res.SeatConfiguration
            WHERE AircraftType = @AircraftType";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@AircraftType", aircraftType);

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error deleting seat configuration for {aircraftType}", ex);
            }
        }
    }
}