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
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _userScope;
    private readonly UserManager<ApplicationUser> _userManager;

    public ReportsController(
        ApplicationDbContext context,
        IUserScopeService userScope,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userScope = userScope;
        _userManager = userManager;
    }

    // GET: Reports
    public async Task<IActionResult> Index(string reportType = "Pipeline", DateTime? startDate = null, DateTime? endDate = null, string? selectedUser = null)
    {
        var model = new ReportSummaryViewModel
        {
            ReportType = reportType,
            StartDate = startDate,
            EndDate = endDate,
            SelectedUser = selectedUser
        };

        var users = await _userManager.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
        ViewBag.UsersList = new SelectList(users, "Id", "FullName", selectedUser);

        switch (reportType.ToLower())
        {
            case "customer":
                var custQuery = _userScope.FilterCustomers(_context.Customers.Include(c => c.AssignedUser).AsNoTracking());
                if (startDate.HasValue) custQuery = custQuery.Where(c => c.CreatedDate >= startDate.Value);
                if (endDate.HasValue) custQuery = custQuery.Where(c => c.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));
                if (!string.IsNullOrEmpty(selectedUser)) custQuery = custQuery.Where(c => c.AssignedTo == selectedUser);
                model.Customers = await custQuery.OrderByDescending(c => c.CreatedDate).ToListAsync();
                break;

            case "lead":
                var leadQuery = _userScope.FilterLeads(_context.Leads.Include(l => l.AssignedUser).Include(l => l.Customer).AsNoTracking());
                if (startDate.HasValue) leadQuery = leadQuery.Where(l => l.CreatedDate >= startDate.Value);
                if (endDate.HasValue) leadQuery = leadQuery.Where(l => l.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));
                if (!string.IsNullOrEmpty(selectedUser)) leadQuery = leadQuery.Where(l => l.AssignedTo == selectedUser);
                model.Leads = await leadQuery.OrderByDescending(l => l.CreatedDate).ToListAsync();
                break;

            case "followup":
                var followQuery = _userScope.FilterFollowUps(_context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsNoTracking());
                if (startDate.HasValue) followQuery = followQuery.Where(f => f.FollowUpDate >= startDate.Value);
                if (endDate.HasValue) followQuery = followQuery.Where(f => f.FollowUpDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));
                if (!string.IsNullOrEmpty(selectedUser)) followQuery = followQuery.Where(f => f.AssignedTo == selectedUser);
                model.FollowUps = await followQuery.OrderBy(f => f.FollowUpDate).ToListAsync();
                break;

            case "opportunity":
                var oppQuery = _userScope.FilterOpportunities(_context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser).AsNoTracking());
                if (startDate.HasValue) oppQuery = oppQuery.Where(o => o.CreatedDate >= startDate.Value);
                if (endDate.HasValue) oppQuery = oppQuery.Where(o => o.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));
                if (!string.IsNullOrEmpty(selectedUser)) oppQuery = oppQuery.Where(o => o.AssignedTo == selectedUser);
                model.Opportunities = await oppQuery.OrderByDescending(o => o.CreatedDate).ToListAsync();
                break;

            case "conversion":
                var allLeads = await _userScope.FilterLeads(_context.Leads.AsNoTracking()).ToListAsync();
                model.TotalLeadsCount = allLeads.Count;
                model.ConvertedLeadsCount = allLeads.Count(l => l.Status == "Converted");
                model.StatusBreakdown = allLeads.GroupBy(l => l.Status).ToDictionary(g => g.Key, g => g.Count());
                break;

            case "useractivity":
                if (_userScope.CanAccessAllRecords())
                {
                    var acts = await _context.Activities.Include(a => a.AssignedUser).AsNoTracking().ToListAsync();
                    model.Activities = acts;
                    model.UserActivityCounts = acts.Where(a => a.AssignedUser != null)
                        .GroupBy(a => a.AssignedUser!.FullName)
                        .ToDictionary(g => g.Key, g => g.Count());
                }
                break;

            case "audit":
                if (_userScope.IsAdmin())
                {
                    model.AuditLogs = await _context.AuditLogs.OrderByDescending(a => a.CreatedDate).Take(100).ToListAsync();
                }
                break;

            case "pipeline":
            default:
                var pipeQuery = _userScope.FilterOpportunities(_context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser).AsNoTracking());
                if (startDate.HasValue) pipeQuery = pipeQuery.Where(o => o.CreatedDate >= startDate.Value);
                if (endDate.HasValue) pipeQuery = pipeQuery.Where(o => o.CreatedDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));
                if (!string.IsNullOrEmpty(selectedUser)) pipeQuery = pipeQuery.Where(o => o.AssignedTo == selectedUser);

                model.Opportunities = await pipeQuery.OrderByDescending(o => o.Amount).ToListAsync();
                model.TotalPipeline = model.Opportunities.Where(o => o.Stage != "Lost").Sum(o => o.Amount);
                model.WeightedPipeline = model.Opportunities.Where(o => o.Stage != "Lost").Sum(o => o.Amount * o.Probability / 100m);
                model.StageBreakdown = model.Opportunities.GroupBy(o => o.Stage).ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
                break;
        }

        return View(model);
    }
}
