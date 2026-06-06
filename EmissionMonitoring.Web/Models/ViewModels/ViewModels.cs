using System.ComponentModel.DataAnnotations;
using EmissionMonitoring.Web.Models.Entities;

namespace EmissionMonitoring.Web.Models.ViewModels;

// ═══════════════════════════════════════════════════
// AUTH ViewModels
// ═══════════════════════════════════════════════════

public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email    { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public class RegisterViewModel
{
    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email    { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // Role selection on register
    [Required]
    [Display(Name = "Role")]
    public string Role { get; set; } = "Operator";

    public List<string> AvailableRoles { get; set; } = new() { "Admin", "Operator", "Viewer" };
}

// ═══════════════════════════════════════════════════
// DASHBOARD ViewModel
// ═══════════════════════════════════════════════════

public class DashboardViewModel
{
    // Plant info
    public string PlantName        { get; set; } = string.Empty;
    public string PlantType        { get; set; } = string.Empty;
    public double NoxSafeLimit     { get; set; }
    public double NoxWarningLimit  { get; set; }
    public double NoxCriticalLimit { get; set; }

    // Latest reading
    public double? LatestFuelConsumption { get; set; }
    public double? LatestProductionLoad  { get; set; }
    public double? LatestTemperature     { get; set; }
    public double? LatestCurrentNox      { get; set; }
    public DateTime? LatestReadingTime   { get; set; }

    // Latest prediction
    public double? PredictedNox    { get; set; }
    public string  RiskLevel       { get; set; } = "N/A";
    public string  AlertMessage    { get; set; } = string.Empty;
    public double? ModelConfidence { get; set; }

    // Summary counts
    public int TotalReadingsToday  { get; set; }
    public int ActiveAlertsCount   { get; set; }
    public int CriticalAlertsToday { get; set; }

    // Last 24h chart data (for Chart.js)
    public List<string> ChartLabels       { get; set; } = new();
    public List<double> ChartCurrentNox   { get; set; } = new();
    public List<double> ChartPredictedNox { get; set; } = new();

    // Recent alerts for dashboard widget
    public List<Alert> RecentAlerts { get; set; } = new();

    // ML service status
    public bool MlServiceOnline { get; set; } = false;
}

// ═══════════════════════════════════════════════════
// PLANT READING ViewModels
// ═══════════════════════════════════════════════════

public class SubmitReadingViewModel
{
    [Required]
    [Display(Name = "Fuel Consumption (kg/hr)")]
    [Range(150, 650, ErrorMessage = "Must be between 150 and 650 kg/hr")]
    public double? FuelConsumption { get; set; }

    [Required]
    [Display(Name = "Production Load (%)")]
    [Range(20, 100, ErrorMessage = "Must be between 20% and 100%")]
    public double? ProductionLoad  { get; set; }

    [Required]
    [Display(Name = "Furnace Temperature (°C)")]
    [Range(500, 1200, ErrorMessage = "Must be between 500°C and 1200°C")]
    public double? Temperature     { get; set; }

    [Required]
    [Display(Name = "Current NOx Level (ppm)")]
    [Range(0, 200, ErrorMessage = "Must be between 0 and 200 ppm")]
    public double? CurrentNox      { get; set; }

    [Required]
    [Display(Name = "Reading Timestamp")]
    public DateTime ReadingTimestamp { get; set; } = DateTime.Now;

    // Returned after prediction
    public PredictionResultViewModel? PredictionResult { get; set; }
}

public class PredictionResultViewModel
{
    public double PredictedNox    { get; set; }
    public string RiskLevel       { get; set; } = string.Empty;
    public string AlertMessage    { get; set; } = string.Empty;
    public double ModelConfidence { get; set; }
    public int    ReadingId       { get; set; }
    public int    PredictionId    { get; set; }
}

public class ReadingsListViewModel
{
    public List<ReadingRowViewModel> Readings    { get; set; } = new();
    public int  TotalCount    { get; set; }
    public int  CurrentPage   { get; set; } = 1;
    public int  PageSize      { get; set; } = 20;
    public int  TotalPages    => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class ReadingRowViewModel
{
    public int      ReadingId         { get; set; }
    public DateTime ReadingTimestamp  { get; set; }
    public double FuelConsumption   { get; set; }
    public double ProductionLoad    { get; set; }
    public double Temperature       { get; set; }
    public double CurrentNox        { get; set; }
    public double?  PredictedNox      { get; set; }
    public string   RiskLevel         { get; set; } = "Pending";
    public string   EnteredByName     { get; set; } = string.Empty;
}

// ═══════════════════════════════════════════════════
// ALERTS ViewModels
// ═══════════════════════════════════════════════════

public class AlertsListViewModel
{
    public List<AlertRowViewModel> Alerts      { get; set; } = new();
    public int  TotalActive    { get; set; }
    public int  TotalWarning   { get; set; }
    public int  TotalCritical  { get; set; }
    public string FilterStatus { get; set; } = "All";  // All | Active | Acknowledged
}

public class AlertRowViewModel
{
    public int      AlertId           { get; set; }
    public string   Severity          { get; set; } = string.Empty;
    public string   Message           { get; set; } = string.Empty;
    public double   PredictedNox      { get; set; }
    public bool     IsAcknowledged    { get; set; }
    public string?  AcknowledgedBy    { get; set; }
    public DateTime? AcknowledgedAt   { get; set; }
    public DateTime CreatedAt         { get; set; }
}

// ═══════════════════════════════════════════════════
// ANALYTICS ViewModels
// ═══════════════════════════════════════════════════

public class AnalyticsViewModel
{
    // Date range filter
    public DateTime FromDate { get; set; } = DateTime.Today.AddDays(-7);
    public DateTime ToDate   { get; set; } = DateTime.Today;

    // Chart.js data
    public List<string> Labels           { get; set; } = new();
    public List<double> CurrentNoxData   { get; set; } = new();
    public List<double> PredictedNoxData { get; set; } = new();
    public List<double> FuelData         { get; set; } = new();
    public List<double> LoadData         { get; set; } = new();

    // Summary stats
    public double AvgCurrentNox   { get; set; }
    public double AvgPredictedNox { get; set; }
    public double MaxCurrentNox   { get; set; }
    public double MaxPredictedNox { get; set; }
    public int    TotalReadings   { get; set; }
    public int    TotalAlerts     { get; set; }
    public int    CriticalCount   { get; set; }
    public int    WarningCount    { get; set; }
    public int    SafeCount       { get; set; }
}

// ═══════════════════════════════════════════════════
// CONFIG ViewModels
// ═══════════════════════════════════════════════════

public class PlantConfigViewModel
{
    public int    PlantId         { get; set; }

    [Required]
    [Display(Name = "Plant Name")]
    public string PlantName       { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Location")]
    public string Location        { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Plant Type")]
    public string PlantType       { get; set; } = string.Empty;

    [Required]
    [Range(10, 150)]
    [Display(Name = "Safe NOx Limit (ppm)")]
    public double NoxSafeLimit    { get; set; } = 80.0;

    [Required]
    [Range(20, 200)]
    [Display(Name = "Warning NOx Limit (ppm)")]
    public double NoxWarningLimit { get; set; } = 100.0;

    [Required]
    [Range(30, 250)]
    [Display(Name = "Critical NOx Limit (ppm)")]
    public double NoxCriticalLimit { get; set; } = 120.0;

    public List<string> PlantTypes { get; set; } = new()
    {
        "Refinery", "PowerPlant", "GasProcessing"
    };
}
