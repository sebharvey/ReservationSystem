using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Res.Domain.Entities.Pnr;
using System.Text.Json;

namespace Res.Infrastructure.Data
{
    public class PnrContext : DbContext
    {
        public DbSet<Pnr> Pnrs { get; set; }

        private readonly JsonSerializerOptions _jsonOptions;

        public PnrContext(DbContextOptions<PnrContext> options) : base(options)
        {
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pnr>(entity =>
            {
                entity.ToTable("Pnrs", schema: "res");

                // Primary key
                entity.HasKey(e => e.Id);

                // Properties
                entity.Property(e => e.RecordLocator);

                entity.Property(e => e.RecordLocator)
                    .HasMaxLength(6)
                    .IsRequired();

                entity.Property(e => e.JsonData)
                    .IsRequired()
                    .HasColumnType("nvarchar(max)");

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UpdatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.SessionId);
                entity.Property(e => e.SessionTimestamp);

                // Ignore the navigation property for the deserialized data
                entity.Ignore(e => e.Data);

                // Add indexes
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.UpdatedAt);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update timestamps before saving
            var entries = ChangeTracker.Entries<Pnr>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                }
                entry.Entity.UpdatedAt = DateTime.UtcNow;

                // Serialize the Data property to JsonData if it exists
                if (entry.Entity.Data != null)
                {
                    entry.Entity.JsonData = JsonSerializer.Serialize(entry.Entity.Data, _jsonOptions);
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
    
    // Extension methods for common queries
    public static class PnrContextExtensions
    {
        public static async Task<Pnr> GetPnrById(this PnrContext context, Guid id)
        {
            var record = await context.Pnrs.FirstOrDefaultAsync(p => p.Id == id);

            return record;
        }

        public static async Task<Pnr> GetBySessionId(this PnrContext context, Guid userContextSessionId)
        {
            var record = await context.Pnrs.FirstOrDefaultAsync(p => p.SessionId == userContextSessionId);

            return record;
        }

        // TODO we need to deprecate this, this is will load everything, which we cannot allow during heavy usage
        public static async Task<List<Pnr>> GetAll(this PnrContext context)
        {
            var records = await context.Pnrs.Where(item => !string.IsNullOrWhiteSpace(item.RecordLocator)).ToListAsync();

            return records;
        }

        public static async Task<Pnr> GetPnrByRecordLocator(this PnrContext context, string recordLocator)
        {
            var record = await context.Pnrs.FirstOrDefaultAsync(p => p.RecordLocator == recordLocator);

            return record;
        }

        public static async Task<List<Pnr>> GetPnrsByLastName(this PnrContext context, string lastName, string firstName = null)
        {
            // Build the SQL query using JSON_VALUE to search within the JsonData
            var query = context.Pnrs.FromSqlRaw(@"
                SELECT *
                FROM res.Pnrs p
                WHERE EXISTS (
                    SELECT 1
                    FROM OPENJSON(p.JsonData, '$.Passengers')
                    WITH (
                        LastName nvarchar(50) '$.LastName',
                        FirstName nvarchar(50) '$.FirstName'
                    )
                    WHERE LastName = @LastName
                    AND (@FirstName IS NULL OR FirstName = @FirstName)
                )",
                new SqlParameter("@LastName", lastName),
                new SqlParameter("@FirstName", firstName ?? (object)DBNull.Value));

            var records = await query.ToListAsync();

            return records;
        }

        public static async Task<bool> SavePnr(this PnrContext context, Pnr pnr)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            // Check if record exists
            var existing = await context.Pnrs.FindAsync(pnr.Id);

            if (existing != null)
            {
                // Existing PNR

                existing.Data = pnr.Data;
                context.Pnrs.Update(existing);
            }
            else
            {
                // New PNR

                pnr.Id = new Guid();
                await context.Pnrs.AddAsync(pnr);
            }

            await context.SaveChangesAsync();
            return true;
        }

        public static async Task<bool> DeletePnr(this PnrContext context, string recordLocator)
        {
            var record = await context.Pnrs.FindAsync(recordLocator);
            if (record == null) return false;

            context.Pnrs.Remove(record);
            await context.SaveChangesAsync();
            return true;
        }
    }
}