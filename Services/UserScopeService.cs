using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using AcxiomCRM.Models;

namespace AcxiomCRM.Services;

public interface IUserScopeService
{
    string? GetCurrentUserId();
    bool IsAdmin();
    bool IsManager();
    bool IsSalesExecutive();
    bool CanAccessAllRecords();
    IQueryable<Customer> FilterCustomers(IQueryable<Customer> query);
    IQueryable<Lead> FilterLeads(IQueryable<Lead> query);
    IQueryable<Opportunity> FilterOpportunities(IQueryable<Opportunity> query);
    IQueryable<FollowUp> FilterFollowUps(IQueryable<FollowUp> query);
    IQueryable<Activity> FilterActivities(IQueryable<Activity> query);
}

public class UserScopeService : IUserScopeService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserScopeService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? GetCurrentUserId()
    {
        return User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public bool IsAdmin() => User?.IsInRole("Admin") ?? false;
    public bool IsManager() => User?.IsInRole("Manager") ?? false;
    public bool IsSalesExecutive() => User?.IsInRole("SalesExecutive") ?? false;

    public bool CanAccessAllRecords() => IsAdmin() || IsManager();

    public IQueryable<Customer> FilterCustomers(IQueryable<Customer> query)
    {
        if (CanAccessAllRecords()) return query;
        var userId = GetCurrentUserId();
        return query.Where(c => c.AssignedTo == userId);
    }

    public IQueryable<Lead> FilterLeads(IQueryable<Lead> query)
    {
        if (CanAccessAllRecords()) return query;
        var userId = GetCurrentUserId();
        return query.Where(l => l.AssignedTo == userId);
    }

    public IQueryable<Opportunity> FilterOpportunities(IQueryable<Opportunity> query)
    {
        if (CanAccessAllRecords()) return query;
        var userId = GetCurrentUserId();
        return query.Where(o => o.AssignedTo == userId);
    }

    public IQueryable<FollowUp> FilterFollowUps(IQueryable<FollowUp> query)
    {
        if (CanAccessAllRecords()) return query;
        var userId = GetCurrentUserId();
        return query.Where(f => f.AssignedTo == userId);
    }

    public IQueryable<Activity> FilterActivities(IQueryable<Activity> query)
    {
        if (CanAccessAllRecords()) return query;
        var userId = GetCurrentUserId();
        return query.Where(a => a.AssignedTo == userId);
    }
}
