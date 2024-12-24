using Microsoft.Extensions.Logging;
using Res.Domain.Entities.Pnr;
using Res.Infrastructure.Data;
using System.Text.Json;

namespace Res.Infrastructure.Repositories
{
    public class PnrRepository : IPnrRepository
    {
        private readonly PnrContext _context;
        private readonly ILogger<PnrRepository> _logger;

        public PnrRepository(PnrContext context, ILogger<PnrRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        //public List<Pnr> Pnrs
        //{
        //    get
        //    {
        //        return _context.Pnrs.ToList().Select(item => item.DeserializePnr()).ToList();
        //    }
        //    set => throw new NotSupportedException("Direct set of PNRs collection is not supported in database mode");
        //}

        public async Task<Pnr> GetByRecordLocator(string recordLocator)
        {
            try
            {
                var record = await _context.GetPnrByRecordLocator(recordLocator);

                return record.DeserializePnr();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PNR {RecordLocator}", recordLocator);
                throw;
            }
        }

        public async Task<List<Pnr>> GetAll()
        {
            try
            {
                var records = await _context.GetAll();

                return records.Select(item => item.DeserializePnr()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PNRs");
                throw;
            }
        }

        public async Task<Pnr?> GetBySessionId(Guid userContextSessionId)
        {
            try
            {
                var record = await _context.GetBySessionId(userContextSessionId);

                return record.DeserializePnr();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PNR {userContextSessionId}", userContextSessionId);
                throw;
            }
        }

        public async Task<List<Pnr>> GetByLastName(string lastName, string firstName = null)
        {
            try
            {
                var records = await _context.GetPnrsByLastName(lastName, firstName);

                return records.Select(item => item.DeserializePnr()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PNRs for {LastName}, {FirstName}", lastName, firstName);
                throw;
            }
        }

        //public async Task<List<Pnr>> GetByFlight(string flightNumber, string date)
        //{
        //    try
        //    {
        //        return await _context.GetPnrsByFlight(flightNumber, date);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving PNRs for flight {FlightNumber} on {Date}", flightNumber, date);
        //        throw;
        //    }
        //}

        //public async Task<List<Pnr>> GetByPhone(string phoneNumber)
        //{
        //    try
        //    {
        //        return await _context.GetPnrsByPhone(phoneNumber);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving PNRs for phone {PhoneNumber}", phoneNumber);
        //        throw;
        //    }
        //}

        //public async Task<List<Pnr>> GetByTicket(string ticketNumber)
        //{
        //    try
        //    {
        //        return await _context.GetPnrsByTicket(ticketNumber);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving PNRs for ticket {TicketNumber}", ticketNumber);
        //        throw;
        //    }
        //}

        //public async Task<List<Pnr>> GetByFrequentFlyer(string ffNumber)
        //{
        //    try
        //    {
        //        return await _context.GetPnrsByFrequentFlyer(ffNumber);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving PNRs for frequent flyer {FFNumber}", ffNumber);
        //        throw;
        //    }
        //}

        public async Task<bool> Save(Pnr pnr, bool commit = false)
        {
            try
            {
                // Check if record exists
                var existing = await _context.GetPnrById(pnr.Id);

                if (existing != null)
                {
                    // Existing PNR

                    if (commit && string.IsNullOrWhiteSpace(existing.RecordLocator))
                        existing.RecordLocator = GenerateRecordLocator();

                    existing.Data = pnr.Data;
                    _context.Pnrs.Update(existing);
                }
                else
                {
                    // New PNR

                    if (commit && string.IsNullOrWhiteSpace(pnr.RecordLocator))
                        pnr.RecordLocator = GenerateRecordLocator();

                    pnr.Id = new Guid();
                    pnr.Data.CreatedDate = DateTime.UtcNow;

                    await _context.Pnrs.AddAsync(pnr);
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding PNR - {ex.Message}");
                throw;
            }
        }

        private string GenerateRecordLocator()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            const int length = 6;
            var random = new Random();
            string recordLocator;

            do
            {
                recordLocator = new string(Enumerable.Repeat(chars, length)
                    .Select(s => s[random.Next(s.Length)]).ToArray());
            } while (_context.Pnrs.Any(p => p.RecordLocator == recordLocator));

            return recordLocator;
        }

        public async Task<bool> Delete(string recordLocator)
        {
            try
            {
                return await _context.DeletePnr(recordLocator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PNR {RecordLocator}", recordLocator);
                throw;
            }
        }

    }

    public static class PnrExtensions
    {
        public static Pnr DeserializePnr(this Pnr pnr)
        {
            if (pnr == null) return null;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            pnr.Data = JsonSerializer.Deserialize<Pnr.PnrData>(pnr.JsonData, options);

            return pnr;
        }
    }
}