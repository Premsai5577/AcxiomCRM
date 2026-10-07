using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class Opportunity : IValidatableObject
{
    [Key]
    public int OpportunityId { get; set; }

    [Required(ErrorMessage = "Opportunity Name is required.")]
    [StringLength(100, ErrorMessage = "Opportunity Name cannot exceed 100 characters.")]
    [Display(Name = "Opportunity Name")]
    public string OpportunityName { get; set; } = string.Empty;

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Required(ErrorMessage = "Opportunity Amount is required.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Opportunity Amount must be greater than 0.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Stage is required.")]
    [StringLength(50)]
    public string Stage { get; set; } = "Qualification"; // Qualification, Proposal, Negotiation, Won, Lost

    [Required(ErrorMessage = "Probability is required.")]
    [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
    public int Probability { get; set; } = 20;

    [Required(ErrorMessage = "Expected Close Date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Expected Close Date")]
    public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Open"; // Open, Won, Lost

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    [NotMapped]
    public decimal WeightedPipeline => Amount * Probability / 100m;

    public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Business Rule: For active opportunities, Expected Close Date cannot be in the past
        if (Stage != "Won" && Stage != "Lost")
        {
            if (ExpectedCloseDate.Date < DateTime.Today)
            {
                yield return new ValidationResult(
                    "Expected Close Date cannot be in the past.",
                    new[] { nameof(ExpectedCloseDate) });
            }

            if (Amount <= 0)
            {
                yield return new ValidationResult(
                    "Opportunity Amount must be greater than 0.",
                    new[] { nameof(Amount) });
            }
        }

        if (Probability < 0 || Probability > 100)
        {
            yield return new ValidationResult(
                "Probability must be between 0 and 100.",
                new[] { nameof(Probability) });
        }
    }
}
