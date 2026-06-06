using Microsoft.EntityFrameworkCore;
using EmissionMonitoring.Web.Data;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Models.ViewModels;
using EmissionMonitoring.Web.Services.Interfaces;

namespace EmissionMonitoring.Web.Services;

/// <summary>
/// Manages the complete alert lifecycle:
///   Create → Display → Acknowledge
///
/// An alert is created when ML predicts Warning or Critical.
/// Operators acknowledge alerts after taking corrective action.
/// </summary>
public class AlertService : IAlertService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AlertService> _logger;

    public AlertService(ApplicationDbContext db, ILogger<AlertService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    /// <summary>
    /// After a prediction is saved, call this.
    /// Creates an Alert record only if RiskLevel is Warning or Critical.
    /// Safe predictions don't generate alerts.
    /// </summary>
    public async Task<Alert?> CreateAlertIfNeededAsync(Prediction prediction, int plantId)
    {
        if (prediction.RiskLevel == "Safe")
            return null;   // No alert needed

        var alert = new Alert
        {
            PredictionId = prediction.PredictionId,
            PlantId      = plantId,
            Severity     = prediction.RiskLevel,
            Message      = prediction.AlertMessage,
            CreatedAt    = DateTime.UtcNow
        };

        _db.Alerts.Add(alert);
        await _db.SaveChangesAsync();

        _logger.LogWarning("Alert created: {Severity} — NOx {Nox} ppm",
            alert.Severity, prediction.PredictedNox);

        return alert;
    }

    /// <summary>
    /// Returns list of alerts for the Alert Center page.
    /// filter: "All" | "Active" | "Acknowledged" | "Critical" | "Warning"
    /// </summary>
    public async Task<AlertsListViewModel> GetAlertsAsync(int plantId, string filter = "All")
    {
        var query = _db.Alerts
            .Where(a => a.PlantId == plantId)
            .Include(a => a.Prediction)
            .Include(a => a.AcknowledgedByUser)
            .OrderByDescending(a => a.CreatedAt);

        var filtered = filter switch
        {
            "Active"       => query.Where(a => !a.IsAcknowledged),
            "Acknowledged" => query.Where(a => a.IsAcknowledged),
            "Critical"     => query.Where(a => a.Severity == "Critical"),
            "Warning"      => query.Where(a => a.Severity == "Warning"),
            _              => query
        };

        var alerts = await filtered.ToListAsync();

        var rows = alerts.Select(a => new AlertRowViewModel
        {
            AlertId         = a.AlertId,
            Severity        = a.Severity,
            Message         = a.Message,
            PredictedNox    = a.Prediction?.PredictedNox ?? 0,
            IsAcknowledged  = a.IsAcknowledged,
            AcknowledgedBy  = a.AcknowledgedByUser?.FullName,
            AcknowledgedAt  = a.AcknowledgedAt,
            CreatedAt       = a.CreatedAt
        }).ToList();

        // Stats for the top summary cards
        var allAlerts = await _db.Alerts.Where(a => a.PlantId == plantId).ToListAsync();

        return new AlertsListViewModel
        {
            Alerts        = rows,
            TotalActive   = allAlerts.Count(a => !a.IsAcknowledged),
            TotalWarning  = allAlerts.Count(a => a.Severity == "Warning" && !a.IsAcknowledged),
            TotalCritical = allAlerts.Count(a => a.Severity == "Critical" && !a.IsAcknowledged),
            FilterStatus  = filter
        };
    }

    /// <summary>
    /// Operator marks alert as acknowledged — like SCADA systems.
    /// </summary>
    public async Task<bool> AcknowledgeAlertAsync(int alertId, string userId)
    {
        var alert = await _db.Alerts.FindAsync(alertId);
        if (alert == null || alert.IsAcknowledged)
            return false;

        alert.IsAcknowledged       = true;
        alert.AcknowledgedByUserId = userId;
        alert.AcknowledgedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("Alert #{Id} acknowledged by {UserId}", alertId, userId);
        return true;
    }

    public async Task<int> GetActiveAlertCountAsync(int plantId)
        => await _db.Alerts.CountAsync(a => a.PlantId == plantId && !a.IsAcknowledged);
}
