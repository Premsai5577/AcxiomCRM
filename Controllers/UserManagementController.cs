using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin")]
public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IAuditService _auditService;

    public UserManagementController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAuditService auditService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _auditService = auditService;
    }

    // GET: UserManagement
    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();
        var model = new List<UserListItemViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLocked = await _userManager.IsLockedOutAsync(user);

            model.Add(new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? "None",
                IsActive = user.IsActive,
                IsLockedOut = isLocked,
                AccessFailedCount = user.AccessFailedCount,
                CreatedDate = user.CreatedDate
            });
        }

        return View(model);
    }

    // GET: UserManagement/Create
    public IActionResult Create()
    {
        ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" });
        return View(new CreateUserViewModel());
    }

    // POST: UserManagement/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" }, model.Role);
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            ModelState.AddModelError(nameof(model.Email), "User with this email already exists.");
            ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" }, model.Role);
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }
            await _userManager.AddToRoleAsync(user, model.Role);

            await _auditService.LogAsync("Create", "User", user.Id, null, user.Email, $"Admin created user {user.Email} with role {model.Role}");
            TempData["Success"] = "User created successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" }, model.Role);
        return View(model);
    }

    // GET: UserManagement/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var currentRole = roles.FirstOrDefault() ?? "SalesExecutive";

        var model = new EditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = currentRole,
            IsActive = user.IsActive
        };

        ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" }, currentRole);
        return View(model);
    }

    // POST: UserManagement/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" }, model.Role);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName = model.FullName;
        user.IsActive = model.IsActive;

        // Role change check
        var currentRoles = await _userManager.GetRolesAsync(user);
        var currentRole = currentRoles.FirstOrDefault() ?? "";
        if (currentRole != model.Role)
        {
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole(model.Role));
            }
            await _userManager.AddToRoleAsync(user, model.Role);
            await _auditService.LogAsync("Role Change", "User", user.Id, currentRole, model.Role, $"Role updated for {user.Email} to {model.Role}");
        }

        // Optional password reset by admin
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var err in resetResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, err.Description);
                }
                ViewBag.Roles = new SelectList(new[] { "Admin", "Manager", "SalesExecutive" }, model.Role);
                return View(model);
            }
            await _auditService.LogAsync("Security", "User", user.Id, null, null, $"Admin reset password for user {user.Email}");
        }

        await _userManager.UpdateAsync(user);

        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: UserManagement/Unlock/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);
            await _auditService.LogAsync("Security", "User", user.Id, "Locked", "Unlocked", $"Admin unlocked account {user.Email}");
            TempData["Success"] = $"Account for {user.Email} unlocked.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: UserManagement/ToggleActive/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user != null)
        {
            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);
            await _auditService.LogAsync("Security", "User", user.Id, null, user.IsActive ? "Active" : "Inactive", $"Admin set user {user.Email} active status to {user.IsActive}");
            TempData["Success"] = $"User {user.Email} is now {(user.IsActive ? "Active" : "Inactive")}.";
        }

        return RedirectToAction(nameof(Index));
    }
}
