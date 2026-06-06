using System.Text;
using System.Text.Json;
using EmissionMonitoring.Web.Models.DTOs;
using EmissionMonitoring.Web.Services.Interfaces;

namespace EmissionMonitoring.Web.Services;

/// <summary>
/// Communicates with the Python Flask ML microservice.
/// Called from ReadingsController after saving a reading.
///
/// Flow:
///   ASP.NET → HTTP POST → Flask /api/predict → JSON response → save to DB
/// </summary>
public class PredictionService : IPredictionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PredictionService> _logger;
    private readonly IConfiguration _config;

    // JSON options — maps snake_case (Python) to PascalCase (C#)
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PredictionService(
        IHttpClientFactory httpClientFactory,
        ILogger<PredictionService> logger,
        IConfiguration config)
    {
        _httpClient = httpClientFactory.CreateClient("FlaskApi");
        _logger     = logger;
        _config     = config;
    }

    /// <summary>
    /// Sends 4 plant parameters to Flask and returns predicted NOx + risk.
    /// </summary>
    public async Task<PredictionResponseDto?> GetPredictionAsync(PredictionRequestDto request)
    {
        try
        {
            // Serialize request to JSON — use snake_case to match Python API
            var payload = new
            {
                fuel_consumption = request.FuelConsumption,
                production_load  = request.ProductionLoad,
                temperature      = request.Temperature,
                current_nox      = request.CurrentNox,
                safe_limit       = request.SafeLimit,
                warning_limit    = request.WarningLimit
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Flask API with: Fuel={F}, Load={L}, Temp={T}, NOx={N}",
                request.FuelConsumption, request.ProductionLoad,
                request.Temperature, request.CurrentNox);

            var response = await _httpClient.PostAsync("/api/predict", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Flask API returned {Code}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result       = JsonSerializer.Deserialize<PredictionResponseDto>(
                                    responseJson, _jsonOptions);

            _logger.LogInformation("Prediction: {NOx} ppm, Risk: {Risk}",
                result?.PredictedNox, result?.RiskLevel);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Flask API is unreachable. Is it running on port 5001?");
            return null;
        }
        catch (TaskCanceledException)
        {
            _logger.LogError("Flask API request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling Flask API");
            return null;
        }
    }

    /// <summary>
    /// Health check — called by Dashboard to show ML service status badge.
    /// </summary>
    public async Task<bool> IsFlaskApiHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
