using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EmissionMonitoring.Web.Data;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Services;
using EmissionMonitoring.Web.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════
// 1. DATABASE — EF Core + SQL Server
// ═══════════════════════════════════════════════
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ═══════════════════════════════════════════════
// 2. IDENTITY — Authentication + Roles
// ═══════════════════════════════════════════════
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase       = false;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail         = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath         = "/Account/Login";
    options.LogoutPath        = "/Account/Logout";
    options.AccessDeniedPath  = "/Account/AccessDenied";
    options.ExpireTimeSpan    = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// ═══════════════════════════════════════════════
// 3. HTTP CLIENT — Python Flask API
// ═══════════════════════════════════════════════
builder.Services.AddHttpClient("FlaskApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["FlaskApi:BaseUrl"] ?? "http://localhost:5001");
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// ═══════════════════════════════════════════════
// 4. APPLICATION SERVICES
// ═══════════════════════════════════════════════
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddScoped<IReadingService,    ReadingService>();
builder.Services.AddScoped<IAlertService,      AlertService>();
builder.Services.AddScoped<IAnalyticsService,  AnalyticsService>();
builder.Services.AddScoped<IPlantService,      PlantService>();
builder.Services.AddScoped<IAuditService,      AuditService>();

// ═══════════════════════════════════════════════
// 5. MVC
// ═══════════════════════════════════════════════
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ═══════════════════════════════════════════════
// 6. MIDDLEWARE PIPELINE
// ═══════════════════════════════════════════════
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ═══════════════════════════════════════════════
// 7. ROUTES — Default to Login page
// ═══════════════════════════════════════════════
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "dashboard",
    pattern: "Dashboard/{action=Index}/{id?}",
    defaults: new { controller = "Dashboard" });

// ═══════════════════════════════════════════════
// 8. SEED ROLES + AUTO MIGRATE
// ═══════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Operator", "Viewer" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

app.Run();
