using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ActivityController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _userScope;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivityController(
        ApplicationDbContext context,
        IUserScopeService userScope,
        IAuditService auditService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userScope = userScope;
        _auditService = auditService;
        _userManager = userManager;
    }

    // GET: Activity
    public async Task<IActionResult> Index(string? activityType, DateTime? activityDate, string? statusFilter, string? assignedFilter)
    {
        var query = _userScope.FilterActivities(_context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser)
            .AsNoTracking());

        if (!string.IsNullOrWhiteSpace(activityType))
        {
            query = query.Where(a => a.ActivityType == activityType);
        }

        if (activityDate.HasValue)
        {
            query = query.Where(a => a.ActivityDate.Date == activityDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(a => a.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(assignedFilter))
        {
            query = query.Where(a => a.AssignedTo == assignedFilter);
        }

        ViewData["ActivityType"] = activityType;
        ViewData["ActivityDate"] = activityDate?.ToString("yyyy-MM-dd");
        ViewData["StatusFilter"] = statusFilter;
        ViewData["AssignedFilter"] = assignedFilter;

        await PopulateUsersDropdown(assignedFilter);
        var activities = await query.OrderByDescending(a => a.ActivityDate).ToListAsync();
        return View(activities);
    }

    // GET: Activity/Create
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        await PopulateDropdowns(customerId, leadId, null);
        var activity = new Activity
        {
            CustomerId = customerId,
            LeadId = leadId,
            ActivityDate = DateTime.Today,
            ActivityType = "Call",
            Status = "Completed",
            AssignedTo = _userScope.GetCurrentUserId()
        };
        return View(activity);
    }

    // POST: Activity/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Activity activity)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(activity.CustomerId, activity.LeadId, activity.AssignedTo);
            return View(activity);
        }

        activity.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrEmpty(activity.AssignedTo))
        {
            activity.AssignedTo = _userScope.GetCurrentUserId();
        }

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Activity", activity.ActivityId.ToString(), null, activity.Subject, $"{activity.ActivityType} activity logged: {activity.Subject}");

        TempData["Success"] = "Activity logged successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Activity/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var activity = await _userScope.FilterActivities(_context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.AssignedUser))
            .FirstOrDefaultAsync(m => m.ActivityId == id);

        if (activity == null) return NotFound();

        return View(activity);
    }

    // POST: Activity/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var activity = await _userScope.FilterActivities(_context.Activities).FirstOrDefaultAsync(a => a.ActivityId == id);
        if (activity != null)
        {
            var subj = activity.Subject;
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Activity", id.ToString(), subj, null, $"Activity '{subj}' deleted");
            TempData["Success"] = "Activity deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns(int? customerId = null, int? leadId = null, string? assignedTo = null)
    {
        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.CustomerId = new SelectList(customers, "CustomerId", "CustomerName", customerId);

        var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
        ViewBag.LeadId = new SelectList(leads, "LeadId", "LeadName", leadId);

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.AssignedTo = new SelectList(users, "Id", "FullName", assignedTo);
    }

    private async Task PopulateUsersDropdown(string? selectedId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
    }
}
