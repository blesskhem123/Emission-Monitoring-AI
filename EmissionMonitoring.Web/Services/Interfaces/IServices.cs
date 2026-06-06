using EmissionMonitoring.Web.Models.DTOs;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Models.ViewModels;

namespace EmissionMonitoring.Web.Services.Interfaces;

/// <summary>
/// Calls Python Flask ML API for NOx predictions.
/// </summary>
public interface IPredictionService
{
    Task<PredictionResponseDto?> GetPredictionAsync(PredictionRequestDto request);
    Task<bool>                   IsFlaskApiHealthyAsync();
}

/// <summary>
/// Handles saving and retrieving plant readings.
/// </summary>
public interface IReadingService
{
    Task<PlantReading>             SaveReadingAsync(SubmitReadingViewModel vm, string userId, int plantId);
    Task<ReadingsListViewModel>    GetReadingsAsync(int plantId, int page = 1, int pageSize = 20);
    Task<PlantReading?>            GetReadingByIdAsync(int readingId);
    Task<List<PlantReading>>       GetRecentReadingsAsync(int plantId, int count = 24);
}

/// <summary>
/// Handles alert creation, retrieval and acknowledgement.
/// </summary>
public interface IAlertService
{
    Task<Alert?>              CreateAlertIfNeededAsync(Prediction prediction, int plantId);
    Task<AlertsListViewModel> GetAlertsAsync(int plantId, string filter = "All");
    Task<bool>                AcknowledgeAlertAsync(int alertId, string userId);
    Task<int>                 GetActiveAlertCountAsync(int plantId);
}

/// <summary>
/// Handles analytics data for Chart.js graphs.
/// </summary>
public interface IAnalyticsService
{
    Task<AnalyticsViewModel> GetAnalyticsAsync(int plantId, DateTime from, DateTime to);
}

/// <summary>
/// Handles plant configuration (NOx thresholds).
/// </summary>
public interface IPlantService
{
    Task<Plant?>  GetPlantAsync(int plantId);
    Task<Plant>   UpdatePlantConfigAsync(PlantConfigViewModel vm);
}

/// <summary>
/// Writes to AuditLogs table.
/// </summary>
public interface IAuditService
{
    Task LogAsync(string? userId, string action, string? description = null, string? ip = null);
}
