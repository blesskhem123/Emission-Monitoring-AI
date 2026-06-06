using System.ComponentModel.DataAnnotations;

namespace EmissionMonitoring.Web.Models.Entities;

public class Plant
{
    [Key]
    public int    PlantId          { get; set; }
    public string PlantName        { get; set; } = string.Empty;
    public string Location         { get; set; } = string.Empty;
    public string PlantType        { get; set; } = string.Empty;
    public double NoxSafeLimit     { get; set; } = 80.0;
    public double NoxWarningLimit  { get; set; } = 100.0;
    public double NoxCriticalLimit { get; set; } = 120.0;
    public bool     IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlantReading>    PlantReadings { get; set; } = new List<PlantReading>();
    public ICollection<Alert>           Alerts        { get; set; } = new List<Alert>();
    public ICollection<ApplicationUser> Users         { get; set; } = new List<ApplicationUser>();
}