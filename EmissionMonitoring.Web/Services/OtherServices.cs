using Microsoft.EntityFrameworkCore;
using EmissionMonitoring.Web.Data;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Models.ViewModels;
using EmissionMonitoring.Web.Services.Interfaces;

namespace EmissionMonitoring.Web.Services;

// ═══════════════════════════════════════════════════════
// ANALYTICS SERVICE
// ═══════════════════════════════════════════════════════

/// <summary>
/// Prepares historical data for Chart.js graphs on Analytics page.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;

    public AnalyticsService(ApplicationDbContext db) => _db = db;

    public async Task<AnalyticsViewModel> GetAnalyticsAsync(
        int plantId, DateTime from, DateTime to)
    {
        var readings = await _db.PlantReadings
            .Where(r => r.PlantId == plantId
                     && r.ReadingTimestamp >= from
                     && r.ReadingTimestamp <= to.AddDays(1))
            .Include(r => r.Prediction)
            .OrderBy(r => r.ReadingTimestamp)
            .ToListAsync();

        // Chart.js labels — "Jun 02 14:00" format
        var labels       = readings.Select(r => r.ReadingTimestamp.ToString("MMM dd HH:mm")).ToList();
        var currentNox   = readings.Select(r => r.CurrentNox).ToList();
        var predictedNox = readings.Select(r => r.Prediction?.PredictedNox ?? 0).ToList();
        var fuelData     = readings.Select(r => r.FuelConsumption).ToList();
        var loadData     = readings.Select(r => r.ProductionLoad).ToList();

        // Alert stats
        var alerts = await _db.Alerts
            .Where(a => a.PlantId == plantId
                     && a.CreatedAt >= from
                     && a.CreatedAt <= to.AddDays(1))
            .ToListAsync();

        return new AnalyticsViewModel
        {
            FromDate         = from,
            ToDate           = to,
            Labels           = labels,
            CurrentNoxData   = currentNox,
            PredictedNoxData = predictedNox,
            FuelData         = fuelData,
            LoadData         = loadData,
            TotalReadings    = readings.Count,
            TotalAlerts      = alerts.Count,
            CriticalCount    = alerts.Count(a => a.Severity == "Critical"),
            WarningCount     = alerts.Count(a => a.Severity == "Warning"),
            SafeCount        = readings.Count - alerts.Count,
            AvgCurrentNox    = readings.Any() ? Math.Round(readings.Average(r => r.CurrentNox), 2) : 0,
            AvgPredictedNox  = readings.Any(r => r.Prediction != null)
                                 ? Math.Round(readings.Where(r => r.Prediction != null)
                                              .Average(r => r.Prediction!.PredictedNox), 2)
                                 : 0,
            MaxCurrentNox    = readings.Any() ? readings.Max(r => r.CurrentNox) : 0,
            MaxPredictedNox  = readings.Any(r => r.Prediction != null)
                                 ? readings.Where(r => r.Prediction != null)
                                           .Max(r => r.Prediction!.PredictedNox)
                                 : 0
        };
    }
}


// ═══════════════════════════════════════════════════════
// PLANT SERVICE
// ═══════════════════════════════════════════════════════

/// <summary>
/// Manages plant configuration — name, type, NOx thresholds.
/// </summary>
public class PlantService : IPlantService
{
    private readonly ApplicationDbContext _db;

    public PlantService(ApplicationDbContext db) => _db = db;

    public async Task<Plant?> GetPlantAsync(int plantId)
        => await _db.Plants.FindAsync(plantId);

    public async Task<Plant> UpdatePlantConfigAsync(PlantConfigViewModel vm)
    {
        var plant = await _db.Plants.FindAsync(vm.PlantId)
            ?? throw new KeyNotFoundException($"Plant #{vm.PlantId} not found");

        plant.PlantName        = vm.PlantName;
        plant.Location         = vm.Location;
        plant.PlantType        = vm.PlantType;
        plant.NoxSafeLimit     = vm.NoxSafeLimit;
        plant.NoxWarningLimit  = vm.NoxWarningLimit;
        plant.NoxCriticalLimit = vm.NoxCriticalLimit;

        await _db.SaveChangesAsync();
        return plant;
    }
}


// ═══════════════════════════════════════════════════════
// AUDIT SERVICE
// ═══════════════════════════════════════════════════════

/// <summary>
/// Writes activity records to AuditLogs table.
/// Call this after any significant user action.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;

    public AuditService(ApplicationDbContext db) => _db = db;

    public async Task LogAsync(
        string? userId, string action,
        string? description = null, string? ip = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId      = userId,
            Action      = action,
            Description = description,
            IpAddress   = ip,
            CreatedAt   = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
