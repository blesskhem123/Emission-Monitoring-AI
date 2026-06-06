using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using EmissionMonitoring.Web.Models.Entities;
using EmissionMonitoring.Web.Models.ViewModels;
using EmissionMonitoring.Web.Services.Interfaces;

namespace EmissionMonitoring.Web.Controllers;

/// <summary>
/// Handles authentication — Login, Register, Logout.
///
/// Flow:
///   Register → Create user in AspNetUsers → Assign role → Login
///   Login    → Cookie auth → Redirect to Dashboard
///   Logout   → Clear cookie → Redirect to Login
/// </summary>
public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser>   _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole>      _roleManager;
    private readonly IAuditService                  _audit;

    public AccountController(
        UserManager<ApplicationUser>   userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole>      roleManager,
        IAuditService                  audit)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _roleManager   = roleManager;
        _audit         = audit;
    }

    // ── GET /Account/Login ──
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ── POST /Account/Login ──
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _signInManager.PasswordSignInAsync(
            vm.Email, vm.Password,
            isPersistent: vm.RememberMe,
            lockoutOnFailure: false);

        if (result.Succeeded)
        {
            var user = await _userManager.FindByEmailAsync(vm.Email);
            await _audit.LogAsync(user?.Id, "Login",
                $"User {vm.Email} logged in",
                HttpContext.Connection.RemoteIpAddress?.ToString());

            return LocalRedirect(returnUrl ?? "/Dashboard");
        }

        ModelState.AddModelError(string.Empty, "Invalid email or password.");
        return View(vm);
    }

    // ── GET /Account/Register ──
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register() => View();

    // ── POST /Account/Register ──
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var user = new ApplicationUser
        {
            FullName  = vm.FullName,
            UserName  = vm.Email,
            Email     = vm.Email,
            // Default plant = 1 (Panipat Refinery — our single plant)
            PlantId   = 1,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, vm.Password);

        if (result.Succeeded)
        {
            // Ensure role exists, then assign
            if (!await _roleManager.RoleExistsAsync(vm.Role))
                await _roleManager.CreateAsync(new IdentityRole(vm.Role));

            await _userManager.AddToRoleAsync(user, vm.Role);

            await _audit.LogAsync(user.Id, "Register",
                $"New user {vm.Email} registered with role {vm.Role}");

            // Auto-login after register
            await _signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction("Index", "Dashboard");
        }

        // Show Identity validation errors
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(vm);
    }

    // ── POST /Account/Logout ──
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userId = _userManager.GetUserId(User);
        await _audit.LogAsync(userId, "Logout");
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }

    // ── GET /Account/AccessDenied ──
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
