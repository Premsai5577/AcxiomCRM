using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _auditService;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService auditService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _auditService = auditService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginApiRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid input data",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            await _auditService.LogAsync("Failed Login", "API Auth", null, null, null, $"API Login failed for {request.Email}");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "Invalid credentials or inactive account."
            });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            var roles = await _userManager.GetRolesAsync(user);
            await _auditService.LogAsync("Login", "API Auth", user.Id, null, null, $"API Login successful for {user.Email}");

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Authentication successful",
                Data = new
                {
                    UserId = user.Id,
                    Email = user.Email,
                    FullName = user.FullName,
                    Roles = roles
                }
            });
        }

        if (result.IsLockedOut)
        {
            await _auditService.LogAsync("Failed Login", "API Auth", user.Id, null, null, $"API User {user.Email} is locked out");
            return StatusCode(423, new ApiResponse<object>
            {
                Success = false,
                Message = "Account locked out due to multiple failed login attempts."
            });
        }

        await _auditService.LogAsync("Failed Login", "API Auth", user.Id, null, null, $"API Invalid password for {user.Email}");
        return Unauthorized(new ApiResponse<object>
        {
            Success = false,
            Message = "Invalid email or password."
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok(new ApiResponse<string>
        {
            Success = true,
            Message = "Logged out successfully"
        });
    }
}
