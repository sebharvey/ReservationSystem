using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Res.Microservices.Inventory.Domain.Entities;

namespace Res.Microservices.Inventory.Infrastructure.Data
{
    public class InventoryDbContext : DbContext
    {
        public DbSet<Flight> Flights { get; set; }
        public DbSet<Allocation> Allocations { get; set; }
        public DbSet<AircraftConfig> AircraftConfigs { get; set; }

        public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("Inv");

            modelBuilder.Entity<Flight>(entity =>
            {
                entity.ToTable("Flight");
                entity.HasKey(e => e.Reference);
                entity.Property(e => e.FlightNo).HasMaxLength(6).IsRequired();
                entity.Property(e => e.From).HasMaxLength(6).IsRequired();
                entity.Property(e => e.To).HasMaxLength(6).IsRequired();
                entity.Property(e => e.AircraftType).HasMaxLength(3).IsRequired();
                entity.Property(e => e.Seats)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions)null))
                    .HasColumnType("nvarchar(max)")
                    .IsRequired();
            });

            modelBuilder.Entity<Allocation>(entity =>
            {
                entity.ToTable("Allocation");
                entity.HasKey(e => e.Reference);
                entity.Property(e => e.Seats).IsRequired();
                entity.HasOne(e => e.Flight)
                    .WithMany()
                    .HasForeignKey(e => e.InventoryReference);
            });

            modelBuilder.Entity<AircraftConfig>(entity =>
            {
                entity.ToTable("AircraftConfig");
                entity.HasKey(e => e.Reference);
                entity.Property(e => e.AircraftType).HasMaxLength(3).IsRequired();
                entity.Property(e => e.Cabins)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, AircraftConfigCabin>>(v, (JsonSerializerOptions)null))
                    .HasColumnType("nvarchar(max)");
            });
        }
    }
}