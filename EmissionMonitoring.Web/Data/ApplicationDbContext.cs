using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EmissionMonitoring.Web.Models.Entities;

namespace EmissionMonitoring.Web.Data;

/// <summary>
/// Main database context.
/// Inherits IdentityDbContext so ASP.NET Identity tables
/// (AspNetUsers, AspNetRoles, etc.) are auto-created.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ── Custom Tables ──
    public DbSet<Plant>        Plants        { get; set; }
    public DbSet<PlantReading> PlantReadings { get; set; }
    public DbSet<Prediction>   Predictions   { get; set; }
    public DbSet<Alert>        Alerts        { get; set; }
    public DbSet<AuditLog>     AuditLogs     { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // Must call — sets up Identity tables

        // ── PlantReading → Prediction : One-to-One ──
        builder.Entity<Prediction>()
            .HasOne(p => p.PlantReading)
            .WithOne(r => r.Prediction)
            .HasForeignKey<Prediction>(p => p.ReadingId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Prediction → Alert : One-to-One ──
        builder.Entity<Alert>()
            .HasOne(a => a.Prediction)
            .WithOne(p => p.Alert)
            .HasForeignKey<Alert>(a => a.PredictionId)
            .OnDelete(DeleteBehavior.NoAction);   // Avoid cascade cycles

        // ── Alert → Plant ──
        builder.Entity<Alert>()
            .HasOne(a => a.Plant)
            .WithMany(p => p.Alerts)
            .HasForeignKey(a => a.PlantId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── PlantReading → Plant ──
        builder.Entity<PlantReading>()
            .HasOne(r => r.Plant)
            .WithMany(p => p.PlantReadings)
            .HasForeignKey(r => r.PlantId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── PlantReading → User ──
        builder.Entity<PlantReading>()
            .HasOne(r => r.EnteredByUser)
            .WithMany()
            .HasForeignKey(r => r.EnteredByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── Alert acknowledgement → User ──
        builder.Entity<Alert>()
            .HasOne(a => a.AcknowledgedByUser)
            .WithMany()
            .HasForeignKey(a => a.AcknowledgedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── AuditLog → User ──
        builder.Entity<AuditLog>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        // ── ApplicationUser → Plant ──
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Plant)
            .WithMany(p => p.Users)
            .HasForeignKey(u => u.PlantId)
            .OnDelete(DeleteBehavior.NoAction);

        // ═══════════════════════════════════════════════
        // SEED DATA — Default plant pre-loaded
        // ═══════════════════════════════════════════════
        builder.Entity<Plant>().HasData(
            new Plant
            {
                PlantId          = 1,
                PlantName        = "Panipat Refinery Unit 1",
                Location         = "Panipat, Haryana",
                PlantType        = "Refinery",
                NoxSafeLimit     = 80.0,
                NoxWarningLimit  = 100.0,
                NoxCriticalLimit = 120.0,
                IsActive         = true,
                CreatedAt        = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
