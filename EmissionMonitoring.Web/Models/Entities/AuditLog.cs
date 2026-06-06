using System.ComponentModel.DataAnnotations;

namespace EmissionMonitoring.Web.Models.Entities;

public class AuditLog
{
    [Key]
    public int     LogId       { get; set; }
    public string? UserId      { get; set; }
    public string  Action      { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IpAddress   { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}