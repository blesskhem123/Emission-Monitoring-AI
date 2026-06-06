using Microsoft.AspNetCore.Identity;

namespace EmissionMonitoring.Web.Models.Entities;

/// <summary>
/// Extended Identity user — adds FullName, PlantId, IsActive.
/// Roles: Admin | Operator | Viewer
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string   FullName  { get; set; } = string.Empty;

    // Which plant does this user belong to?
    public int?     PlantId   { get; set; }
    public Plant?   Plant     { get; set; }

    public bool     IsActive  { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
