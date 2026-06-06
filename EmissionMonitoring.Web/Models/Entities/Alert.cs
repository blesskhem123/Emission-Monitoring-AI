using System.ComponentModel.DataAnnotations;

namespace EmissionMonitoring.Web.Models.Entities;

public class Alert
{
    [Key]
    public int    AlertId      { get; set; }
    public int    PredictionId { get; set; }
    public int    PlantId      { get; set; }
    public string Severity     { get; set; } = string.Empty;
    public string Message      { get; set; } = string.Empty;
    public bool      IsAcknowledged       { get; set; } = false;
    public string?   AcknowledgedByUserId { get; set; }
    public DateTime? AcknowledgedAt       { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Prediction?      Prediction         { get; set; }
    public Plant?           Plant              { get; set; }
    public ApplicationUser? AcknowledgedByUser { get; set; }
}