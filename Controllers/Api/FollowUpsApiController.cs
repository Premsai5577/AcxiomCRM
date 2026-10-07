using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/followups")]
public class FollowUpsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public FollowUpsApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET /api/followups
    [HttpGet]
    public async Task<IActionResult> GetFollowUps([FromQuery] string? status)
    {
        var query = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(f => f.Status == status);
        }

        var list = await query.Select(f => new FollowUpDto
        {
            FollowUpId = f.FollowUpId,
            CustomerId = f.CustomerId,
            CustomerName = f.Customer != null ? f.Customer.CustomerName : null,
            LeadId = f.LeadId,
            LeadName = f.Lead != null ? f.Lead.LeadName : null,
            Subject = f.Subject,
            FollowUpDate = f.FollowUpDate,
            FollowUpType = f.FollowUpType,
            Status = f.Status,
            Remarks = f.Remarks,
            AssignedTo = f.AssignedTo
        }).ToListAsync();

        return Ok(list);
    }

    // POST /api/followups
    [HttpPost]
    public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Validation failed.",
                Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
            });
        }

        // Business rule check: Follow-up date cannot be earlier than today
        if (dto.Status == "Planned" && dto.FollowUpDate.Date < DateTime.Today)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "Follow-up date cannot be earlier than today."
            });
        }

        var followUp = new FollowUp
        {
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Subject = dto.Subject,
            FollowUpDate = dto.FollowUpDate,
            FollowUpType = dto.FollowUpType,
            Status = dto.Status,
            Remarks = dto.Remarks,
            AssignedTo = dto.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "FollowUp (API)", followUp.FollowUpId.ToString(), null, followUp.Subject, $"Follow-up '{followUp.Subject}' created via REST API");

        var resultDto = new FollowUpDto
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            Subject = followUp.Subject,
            FollowUpDate = followUp.FollowUpDate,
            FollowUpType = followUp.FollowUpType,
            Status = followUp.Status,
            Remarks = followUp.Remarks,
            AssignedTo = followUp.AssignedTo
        };

        return CreatedAtAction(nameof(GetFollowUps), new { id = followUp.FollowUpId }, resultDto);
    }
}
