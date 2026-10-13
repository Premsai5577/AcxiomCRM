using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IUserScopeService _userScope;

    public DashboardController(ApplicationDbContext context, IUserScopeService userScope)
    {
        _context = context;
        _userScope = userScope;
    }

    public async Task<IActionResult> Index(string dateFilter = "All")
    {
        var model = new DashboardViewModel
        {
            DateFilter = dateFilter,
            UserName = User.Identity?.Name ?? "User",
            UserRole = _userScope.IsAdmin() ? "Admin" : (_userScope.IsManager() ? "Manager" : "SalesExecutive")
        };

        // Query base sets filtered by user role scope
        var customersQuery = _userScope.FilterCustomers(_context.Customers.AsNoTracking());
        var leadsQuery = _userScope.FilterLeads(_context.Leads.AsNoTracking());
        var oppsQuery = _userScope.FilterOpportunities(_context.Opportunities.AsNoTracking());
        var followUpsQuery = _userScope.FilterFollowUps(_context.FollowUps.AsNoTracking());
        var activitiesQuery = _userScope.FilterActivities(_context.Activities.AsNoTracking());

        // Apply date filter if selected
        DateTime? filterDate = dateFilter.ToLower() switch
        {
            "today" => DateTime.Today,
            "week" => DateTime.Today.AddDays(-7),
            "month" => DateTime.Today.AddDays(-30),
            _ => null
        };

        if (filterDate.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate >= filterDate.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate >= filterDate.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate >= filterDate.Value);
        }

        // Aggregate KPIs
        model.TotalCustomers = await customersQuery.CountAsync();
        model.TotalLeads = await leadsQuery.CountAsync();
        model.OpenLeads = await leadsQuery.CountAsync(l => l.Status != "Lost" && l.Status != "Converted");

        model.TotalOpportunities = await oppsQuery.CountAsync();
        model.OpenOpportunities = await oppsQuery.CountAsync(o => o.Stage != "Won" && o.Stage != "Lost");
        model.WonOpportunities = await oppsQuery.CountAsync(o => o.Stage == "Won");
        model.LostOpportunities = await oppsQuery.CountAsync(o => o.Stage == "Lost");

        var openOpps = await oppsQuery.Where(o => o.Stage != "Won" && o.Stage != "Lost").ToListAsync();
        model.TotalPipelineValue = openOpps.Sum(o => o.Amount);
        model.WeightedPipelineValue = openOpps.Sum(o => o.Amount * o.Probability / 100m);

        // Chart 1: Lead Status breakdown
        var allLeadsStatus = await leadsQuery
            .Select(l => l.Status)
            .ToListAsync();

        var leadsByStatus = allLeadsStatus
            .GroupBy(l => l)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToList();

        string[] standardLeadStatuses = { "New", "Contacted", "Qualified", "Lost", "Converted" };
        foreach (var status in standardLeadStatuses)
        {
            var match = leadsByStatus.FirstOrDefault(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));
            model.LeadStatusCounts[status] = match?.Count ?? 0;
        }

        // Chart 2: Opportunity Pipeline stage amounts (evaluated in-memory for SQLite decimal support)
        var allOppsForStages = await oppsQuery
            .Select(o => new { o.Stage, o.Amount })
            .ToListAsync();

        var oppsByStage = allOppsForStages
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, TotalAmount = g.Sum(x => x.Amount) })
            .ToList();

        string[] standardStages = { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        foreach (var stage in standardStages)
        {
            var match = oppsByStage.FirstOrDefault(x => string.Equals(x.Stage, stage, StringComparison.OrdinalIgnoreCase));
            model.PipelineStageAmounts[stage] = match?.TotalAmount ?? 0m;
        }

        // Chart 3: Monthly Sales / Performance (Last 6 months)
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
        var wonOpps = await oppsQuery
            .Where(o => o.Stage == "Won" && o.CreatedDate >= new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1))
            .ToListAsync();

        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = DateTime.UtcNow.AddMonths(-i);
            var monthName = targetMonth.ToString("MMM yyyy");
            var monthTotal = wonOpps
                .Where(o => o.CreatedDate.Year == targetMonth.Year && o.CreatedDate.Month == targetMonth.Month)
                .Sum(o => o.Amount);

            model.MonthlySalesData[monthName] = monthTotal;
        }

        // Lists: Upcoming follow-ups and recent activities
        model.UpcomingFollowUps = await followUpsQuery
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Where(f => f.Status == "Planned" && f.FollowUpDate >= DateTime.Today)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        model.RecentActivities = await activitiesQuery
            .OrderByDescending(a => a.ActivityDate)
            .Take(5)
            .ToListAsync();

        model.TopOpportunities = await oppsQuery
            .Include(o => o.Customer)
            .Where(o => o.Stage != "Won" && o.Stage != "Lost")
            .OrderByDescending(o => o.Amount)
            .Take(5)
            .ToListAsync();

        return View(model);
    }
}
