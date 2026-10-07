using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models;

public class FollowUp : IValidatableObject
{
    [Key]
    public int FollowUpId { get; set; }

    [Display(Name = "Customer")]
    public int? CustomerId { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [Display(Name = "Lead")]
    public int? LeadId { get; set; }

    [ForeignKey("LeadId")]
    public virtual Lead? Lead { get; set; }

    [Display(Name = "Opportunity")]
    public int? OpportunityId { get; set; }

    [ForeignKey("OpportunityId")]
    public virtual Opportunity? Opportunity { get; set; }

    [StringLength(50)]
    public string? RelatedType { get; set; } // Customer, Lead, Opportunity

    public int? RelatedId { get; set; }

    [Required(ErrorMessage = "Subject is required.")]
    [StringLength(150, ErrorMessage = "Subject cannot exceed 150 characters.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Follow-up date is required.")]
    [DataType(DataType.Date)]
    [Display(Name = "Follow-Up Date")]
    public DateTime FollowUpDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Follow-up type is required.")]
    [StringLength(50)]
    [Display(Name = "Follow-Up Type")]
    public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

    [StringLength(500)]
    public string? Remarks { get; set; }

    [Display(Name = "Assigned To")]
    public string? AssignedTo { get; set; }

    [ForeignKey("AssignedTo")]
    public virtual ApplicationUser? AssignedUser { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Business Rule: Follow-up date cannot be earlier than today for a new/planned follow-up
        if (Status == "Planned" && FollowUpDate.Date < DateTime.Today)
        {
            yield return new ValidationResult(
                "Follow-up date cannot be earlier than today.",
                new[] { nameof(FollowUpDate) });
        }
    }
}
