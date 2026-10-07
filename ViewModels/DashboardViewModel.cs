using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }

    // Date Filter Selected
    public string DateFilter { get; set; } = "All"; // Today, Week, Month, All

    // Chart Data
    public Dictionary<string, int> LeadStatusCounts { get; set; } = new();
    public Dictionary<string, decimal> PipelineStageAmounts { get; set; } = new();
    public Dictionary<string, decimal> MonthlySalesData { get; set; } = new();

    // Lists
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<Activity> RecentActivities { get; set; } = new();
    public List<Opportunity> TopOpportunities { get; set; } = new();

    // Current User Context
    public string UserRole { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
}
