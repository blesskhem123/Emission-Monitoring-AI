using Microsoft.EntityFrameworkCore;
using EmissionMonitoring.Web.Data;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Models.ViewModels;
using EmissionMonitoring.Web.Services.Interfaces;

namespace EmissionMonitoring.Web.Services;

/// <summary>
/// Handles all plant reading operations:
/// - Save new reading to DB
/// - Fetch paginated reading list
/// - Get recent readings for charts
/// </summary>
public class ReadingService : IReadingService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReadingService> _logger;

    public ReadingService(ApplicationDbContext db, ILogger<ReadingService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    /// <summary>
    /// Saves a new plant reading entered by the operator.
    /// Called BEFORE the ML prediction — reading is saved first,
    /// then prediction is linked to it.
    /// </summary>
    public async Task<PlantReading> SaveReadingAsync(
        SubmitReadingViewModel vm, string userId, int plantId)
    {
        var reading = new PlantReading
        {
            PlantId           = plantId,
            EnteredByUserId   = userId,
            FuelConsumption   = vm.FuelConsumption ?? 0,
            ProductionLoad    = vm.ProductionLoad  ?? 0,
            Temperature       = vm.Temperature     ?? 0,
            CurrentNox        = vm.CurrentNox      ?? 0,
            ReadingTimestamp  = vm.ReadingTimestamp,
            CreatedAt         = DateTime.UtcNow
        };

        _db.PlantReadings.Add(reading);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Reading #{Id} saved for Plant #{PlantId}", reading.ReadingId, plantId);
        return reading;
    }

    /// <summary>
    /// Returns paginated list of readings with their predictions.
    /// </summary>
    public async Task<ReadingsListViewModel> GetReadingsAsync(
        int plantId, int page = 1, int pageSize = 20)
    {
        var query = _db.PlantReadings
            .Where(r => r.PlantId == plantId)
            .Include(r => r.Prediction)
            .Include(r => r.EnteredByUser)
            .OrderByDescending(r => r.ReadingTimestamp);

        var total = await query.CountAsync();

        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReadingRowViewModel
            {
                ReadingId        = r.ReadingId,
                ReadingTimestamp = r.ReadingTimestamp,
                FuelConsumption  = r.FuelConsumption,
                ProductionLoad   = r.ProductionLoad,
                Temperature      = r.Temperature,
                CurrentNox       = r.CurrentNox,
                PredictedNox     = r.Prediction != null ? r.Prediction.PredictedNox : null,
                RiskLevel        = r.Prediction != null ? r.Prediction.RiskLevel : "Pending",
                EnteredByName    = r.EnteredByUser != null ? r.EnteredByUser.FullName : "Unknown"
            })
            .ToListAsync();

        return new ReadingsListViewModel
        {
            Readings    = rows,
            TotalCount  = total,
            CurrentPage = page,
            PageSize    = pageSize
        };
    }

    public async Task<PlantReading?> GetReadingByIdAsync(int readingId)
        => await _db.PlantReadings
            .Include(r => r.Prediction)
            .Include(r => r.Plant)
            .FirstOrDefaultAsync(r => r.ReadingId == readingId);

    /// <summary>
    /// Returns last N readings for dashboard chart (default: last 24 hours).
    /// </summary>
    public async Task<List<PlantReading>> GetRecentReadingsAsync(int plantId, int count = 24)
        => await _db.PlantReadings
            .Where(r => r.PlantId == plantId)
            .Include(r => r.Prediction)
            .OrderByDescending(r => r.ReadingTimestamp)
            .Take(count)
            .OrderBy(r => r.ReadingTimestamp)   // re-sort ascending for chart
            .ToListAsync();
}
