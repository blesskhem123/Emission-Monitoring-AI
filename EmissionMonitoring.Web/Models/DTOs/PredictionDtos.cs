using System.Text.Json.Serialization;

namespace EmissionMonitoring.Web.Models.DTOs;

public class PredictionRequestDto
{
    public double FuelConsumption { get; set; }
    public double ProductionLoad  { get; set; }
    public double Temperature     { get; set; }
    public double CurrentNox      { get; set; }
    public double SafeLimit       { get; set; } = 80.0;
    public double WarningLimit    { get; set; } = 100.0;
}

public class PredictionResponseDto
{
    [JsonPropertyName("success")]
    public bool   Success          { get; set; }

    [JsonPropertyName("predicted_nox")]
    public double PredictedNox     { get; set; }

    [JsonPropertyName("risk_level")]
    public string RiskLevel        { get; set; } = string.Empty;

    [JsonPropertyName("alert_message")]
    public string AlertMessage     { get; set; } = string.Empty;

    [JsonPropertyName("model_confidence")]
    public double ModelConfidence  { get; set; }

    [JsonPropertyName("predicted_at")]
    public string PredictedAt      { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string? Error           { get; set; }
}

public class HealthResponseDto
{
    [JsonPropertyName("status")]
    public string Status      { get; set; } = string.Empty;

    [JsonPropertyName("model_loaded")]
    public bool   ModelLoaded { get; set; }

    [JsonPropertyName("service")]
    public string Service     { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp   { get; set; } = string.Empty;
}