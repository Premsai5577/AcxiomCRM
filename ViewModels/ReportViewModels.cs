using AcxiomCRM.Models;

namespace AcxiomCRM.ViewModels;

public class ReportSummaryViewModel
{
    public string ReportType { get; set; } = "Pipeline";
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SelectedUser { get; set; }

    // Results collections
    public List<Customer> Customers { get; set; } = new();
    public List<Lead> Leads { get; set; } = new();
    public List<Opportunity> Opportunities { get; set; } = new();
    public List<FollowUp> FollowUps { get; set; } = new();
    public List<Activity> Activities { get; set; } = new();
    public List<AuditLog> AuditLogs { get; set; } = new();

    // Aggregations
    public decimal TotalPipeline { get; set; }
    public decimal WeightedPipeline { get; set; }
    public int ConvertedLeadsCount { get; set; }
    public int TotalLeadsCount { get; set; }
    public double ConversionRate => TotalLeadsCount > 0 ? (double)ConvertedLeadsCount / TotalLeadsCount * 100 : 0;

    public Dictionary<string, decimal> StageBreakdown { get; set; } = new();
    public Dictionary<string, int> StatusBreakdown { get; set; } = new();
    public Dictionary<string, int> UserActivityCounts { get; set; } = new();
}
