using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Lead
{
    [Key]
    public int LeadId { get; set; }

    [Display(Name = "Lead Code")]
    [MaxLength(50)]
    public string LeadCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lead Name is required.")]
    [StringLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
    [Display(Name = "Lead Name")]
    public string LeadName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a valid email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter a valid phone number.")]
    [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid phone number (10-digit number).")]
    [Display(Name = "Phone Number")]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "Company Name")]
    [StringLength(100, ErrorMessage = "Company Name cannot exceed 100 characters.")]
    public string? CompanyName { get; set; }

    [StringLength(50)]
    public string? Source { get; set; } // Website, Referral, Cold Call, Partner, Social Media

    [Required(ErrorMessage = "Lead Status is required.")]
    [StringLength(50)]
    public string Status { get; set; } = "New"; // New, Contacted, Qualified, Unqualified, Converted, Lost

    [StringLength(20)]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    [Required(ErrorMessage = "Expected value must be provided.")]
    [Range(0, 100000000, ErrorMessage = "Expected value must be a valid non-negative amount.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Expected Value")]
    public decimal ExpectedValue { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [Display(Name = "Converted Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
    public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
