using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;

namespace AcxiomCRM.Controllers;

[Authorize(Roles = "Admin,Manager")]
public class AuditLogController : Controller
{
    private readonly ApplicationDbContext _context;

    public AuditLogController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: AuditLog
    public async Task<IActionResult> Index(string? userFilter, string? moduleFilter, string? actionFilter, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(userFilter))
        {
            query = query.Where(a => a.UserName != null && a.UserName.Contains(userFilter));
        }

        if (!string.IsNullOrWhiteSpace(moduleFilter))
        {
            query = query.Where(a => a.EntityName == moduleFilter);
        }

        if (!string.IsNullOrWhiteSpace(actionFilter))
        {
            query = query.Where(a => a.Action == actionFilter);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        ViewData["UserFilter"] = userFilter;
        ViewData["ModuleFilter"] = moduleFilter;
        ViewData["ActionFilter"] = actionFilter;
        ViewData["StartDate"] = startDate?.ToString("yyyy-MM-dd");
        ViewData["EndDate"] = endDate?.ToString("yyyy-MM-dd");

        var logs = await query.OrderByDescending(a => a.CreatedDate).Take(150).ToListAsync();
        return View(logs);
    }
}
