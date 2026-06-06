using System.ComponentModel.DataAnnotations;

namespace EmissionMonitoring.Web.Models.Entities;

public class PlantReading
{
    [Key]
    public int    ReadingId       { get; set; }
    public int    PlantId         { get; set; }
    public string EnteredByUserId { get; set; } = string.Empty;

    [Range(150, 650)]
    public double FuelConsumption { get; set; }
    [Range(20, 100)]
    public double ProductionLoad  { get; set; }
    [Range(500, 1200)]
    public double Temperature     { get; set; }
    [Range(0, 200)]
    public double CurrentNox      { get; set; }

    public DateTime ReadingTimestamp { get; set; }
    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;

    public Plant?           Plant         { get; set; }
    public ApplicationUser? EnteredByUser { get; set; }
    public Prediction?      Prediction    { get; set; }
}