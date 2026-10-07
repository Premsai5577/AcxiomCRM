using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels;

public class ConvertLeadViewModel
{
    public int LeadId { get; set; }
    public string LeadCode { get; set; } = string.Empty;
    public string LeadName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public decimal ExpectedValue { get; set; }

    [Display(Name = "Create Customer Record")]
    public bool CreateCustomer { get; set; } = true;

    [Display(Name = "Customer Name")]
    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Display(Name = "Address")]
    public string? Address { get; set; }

    [Display(Name = "City")]
    public string? City { get; set; }

    [Display(Name = "State")]
    public string? State { get; set; }

    [Display(Name = "Also Create Sales Opportunity?")]
    public bool CreateOpportunity { get; set; } = true;

    [Display(Name = "Opportunity Name")]
    public string? OpportunityName { get; set; }

    [Display(Name = "Opportunity Amount")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    public decimal OpportunityAmount { get; set; }

    [Display(Name = "Initial Stage")]
    public string OpportunityStage { get; set; } = "Qualification";

    [Display(Name = "Probability (%)")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int OpportunityProbability { get; set; } = 25;

    [Display(Name = "Expected Close Date")]
    [DataType(DataType.Date)]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);
}
