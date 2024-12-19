using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Res.Domain.Entities.Pnr;

namespace Res.Infrastructure.Data
{
    /*

      CREATE TABLE Pnr (
            RecordLocator VARCHAR(6) PRIMARY KEY,
            Data NVARCHAR(MAX) NOT NULL
        )

        var pnrManager = new PnrDataManager("your_connection_string");

        // Create
        await pnrManager.CreatePnrAsync(pnr);

        // Read
        var pnr = await pnrManager.GetPnrAsync("ABC123");

        // Update
        await pnrManager.UpdatePnrAsync(pnr);

        // Delete
        await pnrManager.DeletePnrAsync("ABC123");

     */

    public class PnrDataManager : IPnrDataManager
    {
        private readonly string _connectionString;
        private readonly JsonSerializerOptions _jsonOptions;

        public PnrDataManager(string connectionString)
        {
            _connectionString = connectionString;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        public async Task<bool> CreatePnrAsync(Pnr pnr)
        {
            if (string.IsNullOrEmpty(pnr.RecordLocator))
                throw new ArgumentException("Record locator is required", nameof(pnr));

            const string sql = @"
                INSERT INTO Pnr (RecordLocator, Data)
                VALUES (@RecordLocator, @Data)";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@RecordLocator", SqlDbType.VarChar, 6).Value = pnr.RecordLocator;
            command.Parameters.Add("@Data", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(pnr, _jsonOptions);

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                // Log exception details here
                throw new DataException($"Error creating PNR {pnr.RecordLocator}", ex);
            }
        }

        public async Task<Pnr> GetPnrAsync(string recordLocator)
        {
            if (string.IsNullOrEmpty(recordLocator))
                throw new ArgumentException("Record locator is required", nameof(recordLocator));

            const string sql = @"
                SELECT Data
                FROM Pnr
                WHERE RecordLocator = @RecordLocator";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@RecordLocator", SqlDbType.VarChar, 6).Value = recordLocator;

            try
            {
                await connection.OpenAsync();
                var result = await command.ExecuteScalarAsync();

                if (result == null)
                    return null;

                return JsonSerializer.Deserialize<Pnr>(result.ToString(), _jsonOptions);
            }
            catch (SqlException ex)
            {
                // Log exception details here
                throw new DataException($"Error retrieving PNR {recordLocator}", ex);
            }
        }

        public async Task<bool> UpdatePnrAsync(Pnr pnr)
        {
            if (string.IsNullOrEmpty(pnr.RecordLocator))
                throw new ArgumentException("Record locator is required", nameof(pnr));

            const string sql = @"
                UPDATE Pnr
                SET Data = @Data
                WHERE RecordLocator = @RecordLocator";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@RecordLocator", SqlDbType.VarChar, 6).Value = pnr.RecordLocator;
            command.Parameters.Add("@Data", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(pnr, _jsonOptions);

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                // Log exception details here
                throw new DataException($"Error updating PNR {pnr.RecordLocator}", ex);
            }
        }

        public async Task<bool> DeletePnrAsync(string recordLocator)
        {
            if (string.IsNullOrEmpty(recordLocator))
                throw new ArgumentException("Record locator is required", nameof(recordLocator));

            const string sql = @"
                DELETE FROM Pnr
                WHERE RecordLocator = @RecordLocator";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@RecordLocator", SqlDbType.VarChar, 6).Value = recordLocator;

            try
            {
                await connection.OpenAsync();
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException ex)
            {
                // Log exception details here
                throw new DataException($"Error deleting PNR {recordLocator}", ex);
            }
        }

        public async Task<bool> PnrExistsAsync(string recordLocator)
        {
            if (string.IsNullOrEmpty(recordLocator))
                throw new ArgumentException("Record locator is required", nameof(recordLocator));

            const string sql = @"
                SELECT COUNT(1)
                FROM Pnr
                WHERE RecordLocator = @RecordLocator";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.Add("@RecordLocator", SqlDbType.VarChar, 6).Value = recordLocator;

            try
            {
                await connection.OpenAsync();
                int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                return count > 0;
            }
            catch (SqlException ex)
            {
                // Log exception details here
                throw new DataException($"Error checking PNR existence {recordLocator}", ex);
            }
        }

        public async Task<List<Pnr>> GetAllPnrsAsync()
        {
            const string sql = @"
            SELECT TOP 50 Data
            FROM Pnr
            ORDER BY RecordLocator";

            var pnrs = new List<Pnr>();

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            try
            {
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var pnr = JsonSerializer.Deserialize<Pnr>(reader.GetString(0), _jsonOptions);
                    pnrs.Add(pnr);
                }

                return pnrs;
            }
            catch (SqlException ex)
            {
                throw new DataException("Error retrieving PNRs", ex);
            }
        }

        public async Task<List<Pnr>> SearchByNameAsync(string lastName, string firstName = null)
        {
            var pnrs = new List<Pnr>();

            // Build SQL query using JSON path queries
            var sql = @"
            SELECT p.Data
            FROM Pnr p
            CROSS APPLY OPENJSON(p.Data, '$.Passengers')
            WITH (
                LastName nvarchar(100) '$.LastName',
                FirstName nvarchar(100) '$.FirstName'
            ) AS Passengers
            WHERE Passengers.LastName LIKE @LastName";

            if (!string.IsNullOrEmpty(firstName))
            {
                sql += " AND Passengers.FirstName LIKE @FirstName";
            }

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            // Add parameters with wildcards for case-insensitive search
            command.Parameters.Add("@LastName", SqlDbType.NVarChar).Value = $"{lastName}";
            if (!string.IsNullOrEmpty(firstName))
            {
                command.Parameters.Add("@FirstName", SqlDbType.NVarChar).Value = $"{firstName}";
            }

            try
            {
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var pnr = JsonSerializer.Deserialize<Pnr>(reader.GetString(0), _jsonOptions);
                    if (!pnrs.Any(p => p.RecordLocator == pnr.RecordLocator)) // Avoid duplicates
                    {
                        pnrs.Add(pnr);
                    }
                }

                return pnrs;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error searching PNRs by name: {ex.Message}", ex);
            }
        }

        public async Task<List<Pnr>> SearchByFlightAsync(string flightNumber, string date)
        {
            var sql = @"
            SELECT p.Data
            FROM Pnr p
            CROSS APPLY OPENJSON(p.Data, '$.Segments')
            WITH (
                FlightNumber nvarchar(10) '$.FlightNumber',
                DepartureDate nvarchar(10) '$.DepartureDate'
            ) AS Segments
            WHERE Segments.FlightNumber = @FlightNumber
            AND Segments.DepartureDate = @DepartureDate";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FlightNumber", flightNumber);
            command.Parameters.AddWithValue("@DepartureDate", date);

            return await ExecuteSearchQuery(connection, command);
        }

        public async Task<List<Pnr>> SearchByPhoneAsync(string phoneNumber)
        {
            var sql = @"
            SELECT p.Data
            FROM Pnr p
            WHERE JSON_VALUE(p.Data, '$.Contact.PhoneNumber') LIKE @PhoneNumber";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@PhoneNumber", $"%{phoneNumber}%");

            return await ExecuteSearchQuery(connection, command);
        }

        public async Task<List<Pnr>> SearchByTicketAsync(string ticketNumber)
        {
            var sql = @"
            SELECT p.Data
            FROM Pnr p
            CROSS APPLY OPENJSON(p.Data, '$.Tickets')
            WITH (
                TicketNumber nvarchar(50) '$.TicketNumber'
            ) AS Tickets
            WHERE Tickets.TicketNumber = @TicketNumber";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@TicketNumber", ticketNumber);

            return await ExecuteSearchQuery(connection, command);
        }

        public async Task<List<Pnr>> SearchByFrequentFlyerAsync(string ffNumber)
        {
            var sql = @"
            SELECT p.Data
            FROM Pnr p
            CROSS APPLY OPENJSON(p.Data, '$.Remarks') AS Remarks
            WHERE Remarks.value LIKE @FFNumber
            AND Remarks.value LIKE '%FREQUENT FLYER%'";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@FFNumber", $"%{ffNumber}%");

            return await ExecuteSearchQuery(connection, command);
        }

        private async Task<List<Pnr>> ExecuteSearchQuery(SqlConnection connection, SqlCommand command)
        {
            var pnrs = new List<Pnr>();

            try
            {
                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var pnr = JsonSerializer.Deserialize<Pnr>(reader.GetString(0), _jsonOptions);
                    if (!pnrs.Any(p => p.RecordLocator == pnr.RecordLocator)) // Avoid duplicates
                    {
                        pnrs.Add(pnr);
                    }
                }

                return pnrs;
            }
            catch (SqlException ex)
            {
                throw new DataException($"Error executing search query: {ex.Message}", ex);
            }
        }
    }
}