using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/opportunities")]
public class OpportunitiesApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public OpportunitiesApiController(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    // GET /api/opportunities
    [HttpGet]
    public async Task<IActionResult> GetOpportunities([FromQuery] string? stage)
    {
        var query = _context.Opportunities.Include(o => o.Customer).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(stage))
        {
            query = query.Where(o => o.Stage == stage);
        }

        var list = await query.Select(o => new OpportunityDto
        {
            OpportunityId = o.OpportunityId,
            OpportunityName = o.OpportunityName,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer != null ? o.Customer.CustomerName : null,
            LeadId = o.LeadId,
            Amount = o.Amount,
            Stage = o.Stage,
            Probability = o.Probability,
            WeightedPipeline = o.Amount * o.Probability / 100m,
            ExpectedCloseDate = o.ExpectedCloseDate,
            Status = o.Status,
            Notes = o.Notes,
            CreatedDate = o.CreatedDate,
            AssignedTo = o.AssignedTo
        }).ToListAsync();

        return Ok(list);
    }

    // POST /api/opportunities
    [HttpPost]
    public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
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

        // Business rules check
        if (dto.Amount <= 0)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Opportunity Amount must be greater than 0." });
        }

        if (dto.Probability < 0 || dto.Probability > 100)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Probability must be between 0 and 100." });
        }

        if (dto.Stage != "Won" && dto.Stage != "Lost" && dto.ExpectedCloseDate.Date < DateTime.Today)
        {
            return BadRequest(new ApiResponse<object> { Success = false, Message = "Expected Close Date cannot be in the past." });
        }

        var opp = new Opportunity
        {
            OpportunityName = dto.OpportunityName,
            CustomerId = dto.CustomerId,
            LeadId = dto.LeadId,
            Amount = dto.Amount,
            Stage = dto.Stage,
            Probability = dto.Probability,
            ExpectedCloseDate = dto.ExpectedCloseDate,
            Status = dto.Stage == "Won" ? "Won" : (dto.Stage == "Lost" ? "Lost" : "Open"),
            Notes = dto.Notes,
            AssignedTo = dto.AssignedTo,
            CreatedDate = DateTime.UtcNow
        };

        _context.Opportunities.Add(opp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("Create", "Opportunity (API)", opp.OpportunityId.ToString(), null, opp.OpportunityName, $"Opportunity {opp.OpportunityName} created via REST API");

        var resultDto = new OpportunityDto
        {
            OpportunityId = opp.OpportunityId,
            OpportunityName = opp.OpportunityName,
            CustomerId = opp.CustomerId,
            LeadId = opp.LeadId,
            Amount = opp.Amount,
            Stage = opp.Stage,
            Probability = opp.Probability,
            WeightedPipeline = opp.Amount * opp.Probability / 100m,
            ExpectedCloseDate = opp.ExpectedCloseDate,
            Status = opp.Status,
            Notes = opp.Notes,
            CreatedDate = opp.CreatedDate,
            AssignedTo = opp.AssignedTo
        };

        return CreatedAtAction(nameof(GetOpportunities), new { id = opp.OpportunityId }, resultDto);
    }
}
