using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Models.ViewModels;
using EmissionMonitoring.Web.Services.Interfaces;

namespace EmissionMonitoring.Web.Controllers;

// ═══════════════════════════════════════════════════════
// ALERTS CONTROLLER
// ═══════════════════════════════════════════════════════

/// <summary>
/// Alert Center — view and acknowledge alerts.
/// Operators can filter by All | Active | Critical | Warning | Acknowledged.
/// </summary>
[Authorize]
public class AlertsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAlertService                _alerts;
    private readonly IAuditService                _audit;

    public AlertsController(
        UserManager<ApplicationUser> userManager,
        IAlertService                alerts,
        IAuditService                audit)
    {
        _userManager = userManager;
        _alerts      = alerts;
        _audit       = audit;
    }

    // ── GET /Alerts ──
    public async Task<IActionResult> Index(string filter = "All")
    {
        var user    = await _userManager.GetUserAsync(User);
        var plantId = user?.PlantId ?? 1;
        var vm      = await _alerts.GetAlertsAsync(plantId, filter);
        return View(vm);
    }

    // ── POST /Alerts/Acknowledge/5 ──
    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var user   = await _userManager.GetUserAsync(User);
        var userId = user?.Id ?? string.Empty;

        var success = await _alerts.AcknowledgeAlertAsync(id, userId);

        if (success)
        {
            await _audit.LogAsync(userId, "AcknowledgeAlert",
                $"Alert #{id} acknowledged");
            TempData["Success"] = $"Alert #{id} acknowledged successfully.";
        }
        else
        {
            TempData["Error"] = "Alert not found or already acknowledged.";
        }

        return RedirectToAction("Index");
    }
}


// ═══════════════════════════════════════════════════════
// ANALYTICS CONTROLLER
// ═══════════════════════════════════════════════════════

/// <summary>
/// Historical trend charts using Chart.js.
/// Operators/Managers can filter by date range.
/// </summary>
[Authorize]
public class AnalyticsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAnalyticsService            _analytics;

    public AnalyticsController(
        UserManager<ApplicationUser> userManager,
        IAnalyticsService            analytics)
    {
        _userManager = userManager;
        _analytics   = analytics;
    }

    // ── GET /Analytics ──
    public async Task<IActionResult> Index(DateTime? from = null, DateTime? to = null)
    {
        var user    = await _userManager.GetUserAsync(User);
        var plantId = user?.PlantId ?? 1;

        var fromDate = from ?? DateTime.Today.AddDays(-7);
        var toDate   = to   ?? DateTime.Today;

        var vm = await _analytics.GetAnalyticsAsync(plantId, fromDate, toDate);
        return View(vm);
    }

    // ── GET /Analytics/ChartData (AJAX endpoint for Chart.js) ──
    [HttpGet]
    public async Task<IActionResult> ChartData(DateTime? from = null, DateTime? to = null)
    {
        var user    = await _userManager.GetUserAsync(User);
        var plantId = user?.PlantId ?? 1;

        var fromDate = from ?? DateTime.Today.AddDays(-7);
        var toDate   = to   ?? DateTime.Today;

        var vm = await _analytics.GetAnalyticsAsync(plantId, fromDate, toDate);

        // Return just chart data as JSON for AJAX refresh
        return Json(new
        {
            labels           = vm.Labels,
            currentNox       = vm.CurrentNoxData,
            predictedNox     = vm.PredictedNoxData,
            fuelConsumption  = vm.FuelData,
            productionLoad   = vm.LoadData
        });
    }
}


// ═══════════════════════════════════════════════════════
// CONFIG CONTROLLER
// ═══════════════════════════════════════════════════════

/// <summary>
/// Plant configuration — Admin only.
/// Update plant name, type, and NOx threshold limits.
/// </summary>
[Authorize(Roles = "Admin")]
public class ConfigController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPlantService                _plant;
    private readonly IAuditService                _audit;

    public ConfigController(
        UserManager<ApplicationUser> userManager,
        IPlantService                plant,
        IAuditService                audit)
    {
        _userManager = userManager;
        _plant       = plant;
        _audit       = audit;
    }

    // ── GET /Config ──
    public async Task<IActionResult> Index()
    {
        var user    = await _userManager.GetUserAsync(User);
        var plantId = user?.PlantId ?? 1;
        var plant   = await _plant.GetPlantAsync(plantId);

        if (plant == null) return NotFound();

        var vm = new PlantConfigViewModel
        {
            PlantId          = plant.PlantId,
            PlantName        = plant.PlantName,
            Location         = plant.Location,
            PlantType        = plant.PlantType,
            NoxSafeLimit     = plant.NoxSafeLimit,
            NoxWarningLimit  = plant.NoxWarningLimit,
            NoxCriticalLimit = plant.NoxCriticalLimit
        };

        return View(vm);
    }

    // ── POST /Config ──
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PlantConfigViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // Validate threshold order
        if (vm.NoxSafeLimit >= vm.NoxWarningLimit)
        {
            ModelState.AddModelError("NoxWarningLimit",
                "Warning limit must be greater than Safe limit.");
            return View(vm);
        }
        if (vm.NoxWarningLimit >= vm.NoxCriticalLimit)
        {
            ModelState.AddModelError("NoxCriticalLimit",
                "Critical limit must be greater than Warning limit.");
            return View(vm);
        }

        await _plant.UpdatePlantConfigAsync(vm);

        var user = await _userManager.GetUserAsync(User);
        await _audit.LogAsync(user?.Id, "UpdateConfig",
            $"NOx limits updated: Safe={vm.NoxSafeLimit}, " +
            $"Warning={vm.NoxWarningLimit}, Critical={vm.NoxCriticalLimit}");

        TempData["Success"] = "Plant configuration updated successfully.";
        return RedirectToAction("Index");
    }
}
