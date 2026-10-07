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
public class OpportunityController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _userScope;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public OpportunityController(
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

    // GET: Opportunity
    public async Task<IActionResult> Index(string? searchName, int? customerFilter, string? stageFilter, string? statusFilter)
    {
        var query = _userScope.FilterOpportunities(_context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedUser)
            .AsNoTracking());

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            query = query.Where(o => o.OpportunityName.Contains(searchName));
        }

        if (customerFilter.HasValue && customerFilter.Value > 0)
        {
            query = query.Where(o => o.CustomerId == customerFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(stageFilter))
        {
            query = query.Where(o => o.Stage == stageFilter);
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(o => o.Status == statusFilter);
        }

        ViewData["SearchName"] = searchName;
        ViewData["CustomerFilter"] = customerFilter;
        ViewData["StageFilter"] = stageFilter;
        ViewData["StatusFilter"] = statusFilter;

        await PopulateCustomersDropdown(customerFilter);
        var opps = await query.OrderByDescending(o => o.CreatedDate).ToListAsync();
        return View(opps);
    }

    // GET: Opportunity/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var opp = await _userScope.FilterOpportunities(_context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.Lead)
            .Include(o => o.AssignedUser)
            .Include(o => o.FollowUps))
            .FirstOrDefaultAsync(m => m.OpportunityId == id);

        if (opp == null) return NotFound();

        return View(opp);
    }

    // GET: Opportunity/Create
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        await PopulateDropdowns(customerId, null, null);
        var model = new Opportunity
        {
            CustomerId = customerId,
            LeadId = leadId,
            Stage = "Qualification",
            Probability = 20,
            Amount = 10000m,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            Status = "Open",
            AssignedTo = _userScope.GetCurrentUserId()
        };
        return View(model);
    }

    // POST: Opportunity/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Opportunity opportunity)
    {
        // Business Rules Enforcement
        if (opportunity.Amount <= 0)
        {
            ModelState.AddModelError(nameof(opportunity.Amount), "Opportunity Amount must be greater than 0.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            ModelState.AddModelError(nameof(opportunity.Probability), "Probability must be between 0 and 100.");
        }

        if (opportunity.Stage != "Won" && opportunity.Stage != "Lost")
        {
            if (opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(opportunity.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
            return View(opportunity);
        }

        opportunity.Status = opportunity.Stage == "Won" ? "Won" : (opportunity.Stage == "Lost" ? "Lost" : "Open");
        opportunity.CreatedDate = DateTime.UtcNow;

        if (string.IsNullOrEmpty(opportunity.AssignedTo))
        {
            opportunity.AssignedTo = _userScope.GetCurrentUserId();
        }

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Opportunity", opportunity.OpportunityId.ToString(), null, opportunity.OpportunityName, $"Opportunity {opportunity.OpportunityName} created with Amount {opportunity.Amount}");

        TempData["Success"] = "Opportunity created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Opportunity/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var opp = await _userScope.FilterOpportunities(_context.Opportunities).FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (opp == null) return NotFound();

        await PopulateDropdowns(opp.CustomerId, opp.LeadId, opp.AssignedTo);
        return View(opp);
    }

    // POST: Opportunity/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Opportunity opportunity)
    {
        if (id != opportunity.OpportunityId) return NotFound();

        var existing = await _userScope.FilterOpportunities(_context.Opportunities).FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (existing == null) return NotFound();

        // Business Rules Enforcement
        if (opportunity.Amount <= 0)
        {
            ModelState.AddModelError(nameof(opportunity.Amount), "Opportunity Amount must be greater than 0.");
        }

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
        {
            ModelState.AddModelError(nameof(opportunity.Probability), "Probability must be between 0 and 100.");
        }

        if (opportunity.Stage != "Won" && opportunity.Stage != "Lost")
        {
            if (opportunity.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(nameof(opportunity.ExpectedCloseDate), "Expected Close Date cannot be in the past.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns(opportunity.CustomerId, opportunity.LeadId, opportunity.AssignedTo);
            return View(opportunity);
        }

        var oldValues = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Probability: {existing.Probability}";

        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.LeadId = opportunity.LeadId;
        existing.Amount = opportunity.Amount;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Notes = opportunity.Notes;
        existing.Status = opportunity.Stage == "Won" ? "Won" : (opportunity.Stage == "Lost" ? "Lost" : "Open");

        if (_userScope.CanAccessAllRecords() && !string.IsNullOrEmpty(opportunity.AssignedTo))
        {
            existing.AssignedTo = opportunity.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newValues = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Probability: {existing.Probability}";
        await _auditService.LogAsync("Update", "Opportunity", id.ToString(), oldValues, newValues, $"Opportunity {existing.OpportunityName} updated");

        TempData["Success"] = "Opportunity updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Opportunity/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var opp = await _userScope.FilterOpportunities(_context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser))
            .FirstOrDefaultAsync(m => m.OpportunityId == id);

        if (opp == null) return NotFound();

        return View(opp);
    }

    // POST: Opportunity/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var opp = await _userScope.FilterOpportunities(_context.Opportunities).FirstOrDefaultAsync(o => o.OpportunityId == id);
        if (opp != null)
        {
            var name = opp.OpportunityName;
            _context.Opportunities.Remove(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Opportunity", id.ToString(), name, null, $"Opportunity {name} deleted");
            TempData["Success"] = "Opportunity deleted successfully.";
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

    private async Task PopulateCustomersDropdown(int? selectedId = null)
    {
        var customers = await _context.Customers.OrderBy(c => c.CustomerName).ToListAsync();
        ViewBag.CustomerList = new SelectList(customers, "CustomerId", "CustomerName", selectedId);
    }
}
