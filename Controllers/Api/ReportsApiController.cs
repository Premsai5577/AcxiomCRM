using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.DTOs;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
public class ReportsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsApiController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET /api/reports/pipeline
    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport()
    {
        var opps = await _context.Opportunities
            .Include(o => o.Customer)
            .Include(o => o.AssignedUser)
            .AsNoTracking()
            .ToListAsync();

        var stageBreakdown = opps
            .GroupBy(o => o.Stage)
            .Select(g => new
            {
                Stage = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.Amount * x.Probability / 100m)
            })
            .ToList();

        var ownerBreakdown = opps
            .GroupBy(o => o.AssignedUser?.FullName ?? "Unassigned")
            .Select(g => new
            {
                Owner = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.Amount * x.Probability / 100m)
            })
            .ToList();

        var totalAmount = opps.Where(o => o.Stage != "Lost").Sum(o => o.Amount);
        var totalWeighted = opps.Where(o => o.Stage != "Lost").Sum(o => o.Amount * o.Probability / 100m);

        return Ok(new
        {
            Success = true,
            Summary = new
            {
                TotalPipeline = totalAmount,
                TotalWeightedPipeline = totalWeighted,
                TotalOpportunities = opps.Count
            },
            ByStage = stageBreakdown,
            ByOwner = ownerBreakdown
        });
    }
}
