using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class AuditLog
{
    [Key]
    public int AuditLogId { get; set; }

    public string? UserId { get; set; }

    [Display(Name = "User Name")]
    [StringLength(100)]
    public string? UserName { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty; // Login, Failed Login, Logout, Create, Update, Delete, Role Change, Security, Convert

    [Required]
    [StringLength(50)]
    [Display(Name = "Module / Entity")]
    public string EntityName { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Record ID")]
    public string? RecordId { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Details { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    [Display(Name = "Timestamp")]
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
