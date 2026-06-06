using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Models.ViewModels;
using EmissionMonitoring.Web.Services.Interfaces;

namespace EmissionMonitoring.Web.Controllers;

/// <summary>
/// Dashboard — first page operator sees after login.
/// Shows: current plant status, latest NOx, prediction, active alerts, trend chart.
/// </summary>
[Authorize]
public class DashboardController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IReadingService              _readingService;
    private readonly IAlertService                _alertService;
    private readonly IPredictionService           _predictionService;
    private readonly IPlantService                _plantService;

    public DashboardController(
        UserManager<ApplicationUser> userManager,
        IReadingService              readingService,
        IAlertService                alertService,
        IPredictionService           predictionService,
        IPlantService                plantService)
    {
        _userManager       = userManager;
        _readingService    = readingService;
        _alertService      = alertService;
        _predictionService = predictionService;
        _plantService      = plantService;
    }

    // ── GET /Dashboard ──
    public async Task<IActionResult> Index()
    {
        var user  = await _userManager.GetUserAsync(User);
        var plantId = user?.PlantId ?? 1;

        // Fetch plant config
        var plant = await _plantService.GetPlantAsync(plantId);

        // Last 24 readings for chart
        var recentReadings = await _readingService.GetRecentReadingsAsync(plantId, 24);

        // Latest reading & prediction
        var latest       = recentReadings.LastOrDefault();
        var latestPred   = latest?.Prediction;

        // Active alert count
        var activeAlerts = await _alertService.GetActiveAlertCountAsync(plantId);

        // Recent alerts for widget (last 5)
        var alertsVm = await _alertService.GetAlertsAsync(plantId, "Active");
        var recent5  = alertsVm.Alerts.Take(5).Select(a => new Alert
        {
            AlertId   = a.AlertId,
            Severity  = a.Severity,
            Message   = a.Message,
            CreatedAt = a.CreatedAt
        }).ToList();

        // ML service health
        var mlOnline = await _predictionService.IsFlaskApiHealthyAsync();

        // Build chart data
        var vm = new DashboardViewModel
        {
            PlantName        = plant?.PlantName        ?? "Unknown Plant",
            PlantType        = plant?.PlantType        ?? "Unknown",
            NoxSafeLimit     = plant?.NoxSafeLimit     ?? 80,
            NoxWarningLimit  = plant?.NoxWarningLimit  ?? 100,
            NoxCriticalLimit = plant?.NoxCriticalLimit ?? 120,

            LatestFuelConsumption = latest?.FuelConsumption,
            LatestProductionLoad  = latest?.ProductionLoad,
            LatestTemperature     = latest?.Temperature,
            LatestCurrentNox      = latest?.CurrentNox,
            LatestReadingTime     = latest?.ReadingTimestamp,

            PredictedNox      = latestPred?.PredictedNox,
            RiskLevel         = latestPred?.RiskLevel ?? "N/A",
            AlertMessage      = latestPred?.AlertMessage ?? "No prediction yet. Submit a reading.",
            ModelConfidence   = latestPred?.ModelConfidence,

            TotalReadingsToday  = recentReadings.Count(r =>
                r.ReadingTimestamp.Date == DateTime.Today),
            ActiveAlertsCount   = activeAlerts,
            CriticalAlertsToday = alertsVm.Alerts.Count(a =>
                a.Severity == "Critical" && a.CreatedAt.Date == DateTime.Today),

            ChartLabels       = recentReadings
                .Select(r => r.ReadingTimestamp.ToString("HH:mm"))
                .ToList(),
            ChartCurrentNox   = recentReadings.Select(r => r.CurrentNox).ToList(),
            ChartPredictedNox = recentReadings
                .Select(r => r.Prediction?.PredictedNox ?? 0)
                .ToList(),

            RecentAlerts  = recent5,
            MlServiceOnline = mlOnline
        };

        return View(vm);
    }
}
