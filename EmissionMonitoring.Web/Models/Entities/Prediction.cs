using System.ComponentModel.DataAnnotations;

namespace EmissionMonitoring.Web.Models.Entities;

public class Prediction
{
    [Key]
    public int    PredictionId    { get; set; }
    public int    ReadingId       { get; set; }
    public double PredictedNox    { get; set; }
    public string RiskLevel       { get; set; } = string.Empty;
    public string AlertMessage    { get; set; } = string.Empty;
    public double? ModelConfidence { get; set; }
    public DateTime PredictedAt   { get; set; } = DateTime.UtcNow;

    public PlantReading? PlantReading { get; set; }
    public Alert?        Alert        { get; set; }
}