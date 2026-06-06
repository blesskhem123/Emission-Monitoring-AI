using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EmissionMonitoring.Web.Data;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Models.DTOs;
using EmissionMonitoring.Web.Models.ViewModels;
using EmissionMonitoring.Web.Services.Interfaces;

namespace EmissionMonitoring.Web.Controllers;

[Authorize]
public class ReadingsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IReadingService              _readings;
    private readonly IPredictionService           _prediction;
    private readonly IAlertService                _alerts;
    private readonly IAuditService                _audit;
    private readonly IPlantService                _plant;
    private readonly ApplicationDbContext         _db;

    public ReadingsController(
        UserManager<ApplicationUser> userManager,
        IReadingService              readings,
        IPredictionService           prediction,
        IAlertService                alerts,
        IAuditService                audit,
        IPlantService                plant,
        ApplicationDbContext         db)
    {
        _userManager = userManager;
        _readings    = readings;
        _prediction  = prediction;
        _alerts      = alerts;
        _audit       = audit;
        _plant       = plant;
        _db          = db;
    }

    // GET /Readings
    public async Task<IActionResult> Index(int page = 1)
    {
        var user    = await _userManager.GetUserAsync(User);
        var plantId = user?.PlantId ?? 1;
        var vm      = await _readings.GetReadingsAsync(plantId, page);
        return View(vm);
    }

    // GET /Readings/Submit
    [Authorize(Roles = "Admin,Operator")]
    public IActionResult Submit()
        => View(new SubmitReadingViewModel
        {
            ReadingTimestamp = DateTime.Now
        });

    // POST /Readings/Submit
    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitReadingViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user    = await _userManager.GetUserAsync(User);
        var plantId = user?.PlantId ?? 1;
        var userId  = user?.Id ?? string.Empty;

        var plant = await _plant.GetPlantAsync(plantId);

        // Step 1: Save reading
        var reading = await _readings.SaveReadingAsync(vm, userId, plantId);

        // Step 2: Call Flask ML API
        var requestDto = new PredictionRequestDto
        {
            FuelConsumption = vm.FuelConsumption ?? 0,
            ProductionLoad  = vm.ProductionLoad  ?? 0,
            Temperature     = vm.Temperature     ?? 0,
            CurrentNox      = vm.CurrentNox      ?? 0,
            SafeLimit       = plant?.NoxSafeLimit    ?? 80.0,
            WarningLimit    = plant?.NoxWarningLimit  ?? 100.0
        };

        var predResponse = await _prediction.GetPredictionAsync(requestDto);

        Prediction? savedPrediction = null;

        if (predResponse != null && predResponse.Success)
        {
            // Step 3: Save prediction
            savedPrediction = new Prediction
            {
                ReadingId       = reading.ReadingId,
                PredictedNox    = predResponse.PredictedNox,
                RiskLevel       = predResponse.RiskLevel,
                AlertMessage    = predResponse.AlertMessage,
                ModelConfidence = predResponse.ModelConfidence,
                PredictedAt     = DateTime.UtcNow
            };

            _db.Predictions.Add(savedPrediction);
            await _db.SaveChangesAsync();

            // Step 4: Create alert if needed
            await _alerts.CreateAlertIfNeededAsync(savedPrediction, plantId);
        }
        else
        {
            TempData["Warning"] = "ML service is offline. Reading saved but prediction unavailable.";
        }

        // Step 5: Audit log
        await _audit.LogAsync(userId, "SubmitReading",
            $"Reading #{reading.ReadingId} submitted. NOx={vm.CurrentNox}ppm.");

        // PRG Pattern — redirect to Result to avoid back button resubmission
        return RedirectToAction("Result", new { readingId = reading.ReadingId });
    }

    // GET /Readings/Result/5
    public async Task<IActionResult> Result(int readingId)
    {
        var reading = await _readings.GetReadingByIdAsync(readingId);
        if (reading == null) return RedirectToAction("Submit");

        var vm = new SubmitReadingViewModel
        {
            FuelConsumption  = reading.FuelConsumption,
            ProductionLoad   = reading.ProductionLoad,
            Temperature      = reading.Temperature,
            CurrentNox       = reading.CurrentNox,
            ReadingTimestamp = reading.ReadingTimestamp,
            PredictionResult = reading.Prediction != null ? new PredictionResultViewModel
            {
                PredictedNox    = reading.Prediction.PredictedNox,
                RiskLevel       = reading.Prediction.RiskLevel,
                AlertMessage    = reading.Prediction.AlertMessage,
                ModelConfidence = reading.Prediction.ModelConfidence ?? 0,
                ReadingId       = reading.ReadingId,
                PredictionId    = reading.Prediction.PredictionId
            } : null
        };

        return View("Submit", vm);
    }

    // GET /Readings/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var reading = await _readings.GetReadingByIdAsync(id);
        if (reading == null) return NotFound();
        return View(reading);
    }
}