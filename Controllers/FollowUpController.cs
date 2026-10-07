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
public class FollowUpController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _userScope;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public FollowUpController(
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

    // GET: FollowUp
    public async Task<IActionResult> Index(DateTime? filterDate, string? statusFilter, string? assignedFilter, string? viewType = "all")
    {
        var query = _userScope.FilterFollowUps(_context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedUser)
            .AsNoTracking());

        if (filterDate.HasValue)
        {
            query = query.Where(f => f.FollowUpDate.Date == filterDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(f => f.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(assignedFilter))
        {
            query = query.Where(f => f.AssignedTo == assignedFilter);
        }

        if (viewType == "upcoming")
        {
            query = query.Where(f => f.Status == "Planned" && f.FollowUpDate.Date >= DateTime.Today);
        }
        else if (viewType == "overdue")
        {
            query = query.Where(f => f.Status == "Planned" && f.FollowUpDate.Date < DateTime.Today);
        }

        ViewData["FilterDate"] = filterDate?.ToString("yyyy-MM-dd");
        ViewData["StatusFilter"] = statusFilter;
        ViewData["AssignedFilter"] = assignedFilter;
        ViewData["ViewType"] = viewType;

        await PopulateUsersDropdown(assignedFilter);
        var followUps = await query.OrderBy(f => f.FollowUpDate).ToListAsync();
        return View(followUps);
    }

    // GET: FollowUp/Create
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? oppId = null)
    {
        await PopulateDropdowns(customerId, leadId, oppId, null);
        var model = new FollowUp
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = oppId,
            FollowUpDate = DateTime.Today.AddDays(1),
            FollowUpType = "Call",
            Status = "Planned",
            AssignedTo = _userScope.GetCurrentUserId()
        };
        return View(model);
    }

    // POST: FollowUp/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUp followUp)
    {
        // Business Rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(followUp.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo);
            return View(followUp);
        }

        followUp.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrEmpty(followUp.AssignedTo))
        {
            followUp.AssignedTo = _userScope.GetCurrentUserId();
        }

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "FollowUp", followUp.FollowUpId.ToString(), null, followUp.Subject, $"Follow-up '{followUp.Subject}' scheduled for {followUp.FollowUpDate:d}");

        TempData["Success"] = "Follow-up scheduled successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: FollowUp/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var followUp = await _userScope.FilterFollowUps(_context.FollowUps).FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp == null) return NotFound();

        await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo);
        return View(followUp);
    }

    // POST: FollowUp/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUp followUp)
    {
        if (id != followUp.FollowUpId) return NotFound();

        var existing = await _userScope.FilterFollowUps(_context.FollowUps).FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (existing == null) return NotFound();

        if (followUp.Status == "Planned" && followUp.FollowUpDate.Date < DateTime.Today)
        {
            ModelState.AddModelError(nameof(followUp.FollowUpDate), "Follow-up date cannot be earlier than today.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(followUp.CustomerId, followUp.LeadId, followUp.OpportunityId, followUp.AssignedTo);
            return View(followUp);
        }

        var oldValues = $"Date: {existing.FollowUpDate:d}, Status: {existing.Status}";

        existing.CustomerId = followUp.CustomerId;
        existing.LeadId = followUp.LeadId;
        existing.OpportunityId = followUp.OpportunityId;
        existing.Subject = followUp.Subject;
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.FollowUpType = followUp.FollowUpType;
        existing.Status = followUp.Status;
        existing.Remarks = followUp.Remarks;

        if (_userScope.CanAccessAllRecords() && !string.IsNullOrEmpty(followUp.AssignedTo))
        {
            existing.AssignedTo = followUp.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newValues = $"Date: {existing.FollowUpDate:d}, Status: {existing.Status}";
        await _auditService.LogAsync("Update", "FollowUp", id.ToString(), oldValues, newValues, $"Follow-up '{existing.Subject}' updated");

        TempData["Success"] = "Follow-up updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: FollowUp/MarkComplete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkComplete(int id, string? completionNotes)
    {
        var followUp = await _userScope.FilterFollowUps(_context.FollowUps).FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp != null)
        {
            followUp.Status = "Completed";
            if (!string.IsNullOrWhiteSpace(completionNotes))
            {
                followUp.Remarks = (followUp.Remarks ?? "") + $" [Completed: {completionNotes}]";
            }
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Update", "FollowUp", id.ToString(), "Planned", "Completed", $"Follow-up '{followUp.Subject}' marked as Completed");
            TempData["Success"] = "Follow-up marked as completed.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: FollowUp/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var followUp = await _userScope.FilterFollowUps(_context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.AssignedUser))
            .FirstOrDefaultAsync(m => m.FollowUpId == id);

        if (followUp == null) return NotFound();

        return View(followUp);
    }

    // POST: FollowUp/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var followUp = await _userScope.FilterFollowUps(_context.FollowUps).FirstOrDefaultAsync(f => f.FollowUpId == id);
        if (followUp != null)
        {
            var subject = followUp.Subject;
            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "FollowUp", id.ToString(), subject, null, $"Follow-up '{subject}' deleted");
            TempData["Success"] = "Follow-up deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns(int? customerId = null, int? leadId = null, int? oppId = null, string? assignedTo = null)
    {
        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.CustomerId = new SelectList(customers, "CustomerId", "CustomerName", customerId);

        var leads = await _context.Leads.OrderBy(l => l.LeadName).ToListAsync();
        ViewBag.LeadId = new SelectList(leads, "LeadId", "LeadName", leadId);

        var opps = await _context.Opportunities.OrderBy(o => o.OpportunityName).ToListAsync();
        ViewBag.OpportunityId = new SelectList(opps, "OpportunityId", "OpportunityName", oppId);

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.AssignedTo = new SelectList(users, "Id", "FullName", assignedTo);
    }

    private async Task PopulateUsersDropdown(string? selectedId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
    }
}
