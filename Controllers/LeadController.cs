using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _userScope;
    private readonly IAuditService _auditService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LeadController(
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

    // GET: Lead
    public async Task<IActionResult> Index(string? searchName, string? searchCompany, string? statusFilter, string? assignedFilter)
    {
        var query = _userScope.FilterLeads(_context.Leads.Include(l => l.AssignedUser).AsNoTracking());

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            query = query.Where(l => l.LeadName.Contains(searchName));
        }

        if (!string.IsNullOrWhiteSpace(searchCompany))
        {
            query = query.Where(l => l.CompanyName != null && l.CompanyName.Contains(searchCompany));
        }

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(l => l.Status == statusFilter);
        }

        if (!string.IsNullOrWhiteSpace(assignedFilter))
        {
            query = query.Where(l => l.AssignedTo == assignedFilter);
        }

        ViewData["SearchName"] = searchName;
        ViewData["SearchCompany"] = searchCompany;
        ViewData["StatusFilter"] = statusFilter;
        ViewData["AssignedFilter"] = assignedFilter;

        await PopulateUsersDropdown(assignedFilter);
        var leads = await query.OrderByDescending(l => l.CreatedDate).ToListAsync();
        return View(leads);
    }

    // GET: Lead/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _userScope.FilterLeads(_context.Leads
            .Include(l => l.AssignedUser)
            .Include(l => l.Customer)
            .Include(l => l.Opportunities)
            .Include(l => l.FollowUps)
            .Include(l => l.Activities))
            .FirstOrDefaultAsync(m => m.LeadId == id);

        if (lead == null) return NotFound();

        return View(lead);
    }

    // GET: Lead/Create
    public async Task<IActionResult> Create()
    {
        await PopulateUsersDropdown();
        var lead = new Lead
        {
            LeadCode = "LEAD-" + (await _context.Leads.CountAsync() + 1001),
            Status = "New",
            Priority = "Medium",
            ExpectedValue = 50000m,
            AssignedTo = _userScope.GetCurrentUserId()
        };
        return View(lead);
    }

    // POST: Lead/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Lead lead)
    {
        if (string.IsNullOrWhiteSpace(lead.LeadCode))
        {
            lead.LeadCode = "LEAD-" + (await _context.Leads.CountAsync() + 1001);
        }

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdown(lead.AssignedTo);
            return View(lead);
        }

        lead.CreatedDate = DateTime.UtcNow;
        if (string.IsNullOrEmpty(lead.AssignedTo))
        {
            lead.AssignedTo = _userScope.GetCurrentUserId();
        }

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Lead", lead.LeadId.ToString(), null, lead.LeadName, $"Lead {lead.LeadName} ({lead.LeadCode}) created");

        TempData["Success"] = "Lead created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Lead/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _userScope.FilterLeads(_context.Leads).FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead == null) return NotFound();

        await PopulateUsersDropdown(lead.AssignedTo);
        return View(lead);
    }

    // POST: Lead/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Lead lead)
    {
        if (id != lead.LeadId) return NotFound();

        var existing = await _userScope.FilterLeads(_context.Leads).FirstOrDefaultAsync(l => l.LeadId == id);
        if (existing == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await PopulateUsersDropdown(lead.AssignedTo);
            return View(lead);
        }

        var oldValues = $"Name: {existing.LeadName}, Status: {existing.Status}, ExpectedValue: {existing.ExpectedValue}";

        existing.LeadName = lead.LeadName;
        existing.Email = lead.Email;
        existing.Phone = lead.Phone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Status = lead.Status;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;
        if (_userScope.CanAccessAllRecords() && !string.IsNullOrEmpty(lead.AssignedTo))
        {
            existing.AssignedTo = lead.AssignedTo;
        }

        await _context.SaveChangesAsync();

        var newValues = $"Name: {existing.LeadName}, Status: {existing.Status}, ExpectedValue: {existing.ExpectedValue}";
        await _auditService.LogAsync("Update", "Lead", id.ToString(), oldValues, newValues, $"Lead {existing.LeadName} updated");

        TempData["Success"] = "Lead updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    // GET: Lead/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _userScope.FilterLeads(_context.Leads.Include(l => l.AssignedUser)).FirstOrDefaultAsync(m => m.LeadId == id);
        if (lead == null) return NotFound();

        return View(lead);
    }

    // POST: Lead/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lead = await _userScope.FilterLeads(_context.Leads).FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead != null)
        {
            var leadName = lead.LeadName;
            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("Delete", "Lead", id.ToString(), leadName, null, $"Lead {leadName} deleted");
            TempData["Success"] = "Lead deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Lead/Convert/5
    public async Task<IActionResult> Convert(int? id)
    {
        if (id == null) return NotFound();

        var lead = await _userScope.FilterLeads(_context.Leads).FirstOrDefaultAsync(l => l.LeadId == id);
        if (lead == null) return NotFound();

        if (lead.Status == "Converted")
        {
            TempData["Error"] = "This lead is already converted.";
            return RedirectToAction(nameof(Details), new { id = lead.LeadId });
        }

        var model = new ConvertLeadViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            CustomerName = !string.IsNullOrWhiteSpace(lead.CompanyName) ? lead.CompanyName : lead.LeadName,
            ExpectedValue = lead.ExpectedValue,
            OpportunityName = $"{lead.LeadName} - Deal",
            OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 50000m
        };

        return View(model);
    }

    // POST: Lead/Convert
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(ConvertLeadViewModel model)
    {
        var lead = await _userScope.FilterLeads(_context.Leads).FirstOrDefaultAsync(l => l.LeadId == model.LeadId);
        if (lead == null) return NotFound();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        Customer? customer = null;
        if (model.CreateCustomer)
        {
            // Check if customer email already exists
            customer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower());
            if (customer == null)
            {
                customer = new Customer
                {
                    CustomerCode = "CUST-" + (await _context.Customers.CountAsync() + 1001),
                    CustomerName = model.CustomerName,
                    Email = lead.Email,
                    Phone = lead.Phone,
                    CompanyName = lead.CompanyName,
                    Address = model.Address,
                    City = model.City,
                    State = model.State,
                    Status = "Active",
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name,
                    AssignedTo = lead.AssignedTo
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                await _auditService.LogAsync("Create", "Customer", customer.CustomerId.ToString(), null, customer.CustomerName, $"Customer created from conversion of Lead {lead.LeadCode}");
            }
            lead.CustomerId = customer.CustomerId;
        }

        if (model.CreateOpportunity && !string.IsNullOrWhiteSpace(model.OpportunityName))
        {
            var opportunity = new Opportunity
            {
                OpportunityName = model.OpportunityName,
                CustomerId = customer?.CustomerId,
                LeadId = lead.LeadId,
                Amount = model.OpportunityAmount,
                Stage = model.OpportunityStage,
                Probability = model.OpportunityProbability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = "Open",
                CreatedDate = DateTime.UtcNow,
                AssignedTo = lead.AssignedTo
            };
            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();
            await _auditService.LogAsync("Create", "Opportunity", opportunity.OpportunityId.ToString(), null, opportunity.OpportunityName, $"Opportunity created from conversion of Lead {lead.LeadCode}");
        }

        lead.Status = "Converted";
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Convert", "Lead", lead.LeadId.ToString(), "Qualified", "Converted", $"Lead {lead.LeadName} ({lead.LeadCode}) converted to Customer and Opportunity");

        TempData["Success"] = "Lead successfully converted!";
        return RedirectToAction(nameof(Details), new { id = lead.LeadId });
    }

    private async Task PopulateUsersDropdown(string? selectedId = null)
    {
        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedId);
    }
}
