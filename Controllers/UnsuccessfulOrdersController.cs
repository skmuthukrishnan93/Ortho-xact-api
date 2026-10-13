using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ortho_xact_api.Models;

namespace Ortho_xact_api.Controllers;

[ApiController]
[Route("api/unsuccessful-orders")]
[Authorize(Roles = "admin")]
public class UnsuccessfulOrdersController(OrthoxactContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1)
    {
        var query = db.ProcedurePostAttempts.AsNoTracking().Where(x => x.Status != "Succeeded");
        return Ok(new { total = await query.CountAsync(), items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((Math.Max(1, page) - 1) * 50).Take(50)
            .Select(x => new { x.Id, x.SalesOrder, x.Status, x.CreatedAt, x.CreatedBy }).ToListAsync() });
    }
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var attempt = await db.ProcedurePostAttempts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (attempt == null) return NotFound();
        return Ok(new {
            attempt, failure = await db.UnsuccessfulSOMasters.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id),
            steps = await db.ProcedurePostSteps.AsNoTracking().Where(x => x.AttemptId == id).OrderBy(x => x.Id).ToListAsync()
        });
    }
}
