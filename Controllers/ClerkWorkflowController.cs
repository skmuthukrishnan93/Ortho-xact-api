using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ortho_xact_api.DTO;
using Ortho_xact_api.Models;
using Ortho_xact_api.Services;
using System.Security.Claims;

namespace Ortho_xact_api.Controllers;

[ApiController]
[Route("Rep")]
[Authorize(Roles = "repclerk,admin")]
public class ClerkWorkflowController(OrthoxactContext db, ProcedureStagingService staging,
    ProcedurePostingService posting) : ControllerBase
{
    [HttpPost("clerkorderdetails")]
    public async Task<IActionResult> Details([FromBody] SalesOrderRequest request)
    {
        var order = ProcedureStagingService.Normalize(request.SalesOrderNumber);
        if (order.Length == 0) return BadRequest(new { message = "A sales order is required." });
        try { await staging.EnsureStaged(order); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        var lines = await db.SorStageDetails.AsNoTracking().Where(x => x.SalesOrder == order)
            .OrderBy(x => x.ProcedureNumber).ThenBy(x => x.Line).ToListAsync();
        var headers = await db.SorHeaders.AsNoTracking().Where(x => x.SalesOrder == order).ToListAsync();
        return Ok(lines.Select(x => LineView(x, headers.Single(h => h.ProcedureNumber == x.ProcedureNumber).Customer)));
    }

    [HttpPost("patientproceduredetails")]
    public async Task<IActionResult> Procedures([FromBody] SalesOrderRequest request)
    {
        var order = ProcedureStagingService.Normalize(request.SalesOrderNumber);
        if (order.Length == 0) return BadRequest(new { message = "A sales order is required." });
        // Details performs any legacy migration. Clients request this after Details.
        var headers = await db.SorHeaders.AsNoTracking().Where(x => x.SalesOrder == order).OrderBy(x => x.ProcedureNumber).ToListAsync();
        var lines = await db.SorStageDetails.AsNoTracking().Where(x => x.SalesOrder == order).ToListAsync();
        return Ok(headers.Select(h => new { h.SalesOrder, h.ProcedureNumber, h.PatientNumber, h.PatientName,
            h.SurgeonName, ProcedureDate = h.ProcedureDate == default ? "" : h.ProcedureDate.ToString("yyyy-MM-dd"), h.CgSetNumber,
            Lines = lines.Where(x => x.ProcedureNumber == h.ProcedureNumber).OrderBy(x => x.Line).Select(x => LineView(x, h.Customer)) }));
    }

    private static object LineView(SorDetailStage x, string customer) => new {
        x.SalesOrder, SalesOrderLine = x.Line, x.ProcedureNumber, x.ParentLine, x.ComponentSequence,
        MbomFlag = x.KitFlag, SetsCode = x.SetCode,
        MstockCode = x.StockCode, MstockDes = x.Description, Mwarehouse = x.Warehouse,
        MorderQty = x.OrderQty, MshipQty = x.DeliveredQty, x.RepUsageQty, RetQty = x.ReturnQty,
        Usage = x.UsageQty, x.Variance, x.Status, Customer = customer,
        ClerkChecked = ProcedureStagingService.IsChecked(x), MissingSavedLine = false
    };

    [HttpPost("clerksave")]
    public Task<IActionResult> Save([FromBody] DeliveryOrderDetailPayload payload) => Update(payload, false);

    [HttpPost("clerkvalidate")]
    public Task<IActionResult> Validate([FromBody] DeliveryOrderDetailPayload payload) => Update(payload, true);

    private async Task<IActionResult> Update(DeliveryOrderDetailPayload payload, bool validate)
    {
        var items = payload.Data;
        if (items == null || items.Count == 0 || items.Select(x => ProcedureStagingService.Normalize(x.SalesOrder)).Distinct().Count() != 1)
            return BadRequest(new { message = "Submit all lines of one sales order." });
        var order = ProcedureStagingService.Normalize(items[0].SalesOrder);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await db.ProcedurePostAttempts.AnyAsync(x => x.SalesOrder == order))
            return Conflict(new { message = "Posting has already started. This order is locked for administrator review." });
        var lines = await db.SorStageDetails.Where(x => x.SalesOrder == order).ToListAsync();
        if (lines.Count == 0 || items.Count != lines.Count || items.Select(x => (x.ProcedureNumber, x.SalesOrderLine)).Distinct().Count() != items.Count ||
            !lines.Select(x => (x.ProcedureNumber, x.Line)).ToHashSet().SetEquals(items.Select(x => (x.ProcedureNumber, x.SalesOrderLine))))
            return BadRequest(new { message = "The request must include every saved line in every procedure exactly once." });
        if (lines.Any(x => x.Status != "RepCompleted" && x.Status != "StoresInProgress" &&
            x.Status != "Completed&ReadyForValidation" && x.Status != "ReadyForValidate" &&
            x.Status != "Send Email To Customer Service" && x.Status != "ReadyToPostSyspro"))
            return Conflict(new { message = "This order is not available for warehouse review." });
        if (!validate)
        {
            foreach (var line in lines.Where(x => x.KitFlag != "P"))
            {
                var dto = items.Single(x => x.SalesOrderLine == line.Line && x.ProcedureNumber == line.ProcedureNumber);
                if (!dto.Visited) continue;
                if (!dto.RetQty.HasValue || dto.RetQty < 0 || dto.RetQty > line.DeliveredQty)
                    return BadRequest(new { message = $"Enter a valid checked return quantity for line {line.Line}." });
                line.ReturnQty = dto.RetQty;
                line.UsageQty = line.DeliveredQty - line.ReturnQty;
                line.Variance = line.RepUsageQty - line.UsageQty;
                line.CheckedAt = DateTime.UtcNow;
            }
        }
        if (validate && !lines.All(ProcedureStagingService.IsChecked))
            return BadRequest(new { message = "Check and save all lines in all procedures before validation." });
        foreach (var group in lines.GroupBy(x => x.ProcedureNumber))
        {
            var allChecked = group.All(ProcedureStagingService.IsChecked);
            var status = !allChecked ? "StoresInProgress" : !validate ? "Completed&ReadyForValidation" :
                group.Any(x => x.KitFlag != "P" && x.RepUsageQty != x.DeliveredQty - x.ReturnQty)
                    ? "Send Email To Customer Service" : "ReadyToPostSyspro";
            foreach (var line in group)
            {
                if (line.KitFlag != "P" && line.ReturnQty.HasValue)
                {
                    line.UsageQty = line.DeliveredQty - line.ReturnQty;
                    line.Variance = line.RepUsageQty - line.UsageQty;
                }
                line.Status = status;
            }
        }
        var legacy = await db.DeliveryOrderDetails.Where(x => x.SalesOrder == order).ToListAsync();
        foreach (var line in legacy)
        {
            var stages = lines.Where(x => x.Line == line.Line).ToList();
            line.Status = stages.All(x => x.Status == stages[0].Status) ? stages[0].Status : "StoresInProgress";
            line.RetQty = stages.All(x => x.ReturnQty.HasValue) ? stages.Sum(x => x.ReturnQty) : null;
            line.Usage = stages.All(x => x.UsageQty.HasValue) ? stages.Sum(x => x.UsageQty) : null;
            line.Variance = stages.All(x => x.Variance.HasValue) ? stages.Sum(x => x.Variance) : null;
            line.ClerkDate = stages.Max(x => x.CheckedAt); line.ClerkName = User.FindFirstValue(ClaimTypes.Name);
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return Ok(new { message = validate ? "Validation completed." : "Saved successfully.", canPost = ProcedureStagingService.CanPost(lines) });
    }

    [HttpPost("posttosyspro")]
    public async Task<IActionResult> Post([FromBody] DeliveryOrderDetailPayload payload)
    {
        if (payload.Data == null || payload.Data.Count == 0 ||
            payload.Data.Select(x => ProcedureStagingService.Normalize(x.SalesOrder)).Distinct().Count() != 1)
            return BadRequest(new { message = "Select one complete sales order to post." });
        try
        {
            var result = await posting.Post(ProcedureStagingService.Normalize(payload.Data[0].SalesOrder),
                User.FindFirstValue(ClaimTypes.Name) ?? "");
            return Ok(new { message = "your order is now successfully posted to Syspro", attemptId = result.AttemptId,
                originalSalesOrder = result.OriginalSalesOrder, createdSalesOrders = result.CreatedSalesOrders });
        }
        catch (ProcedurePostingException ex)
        {
            return Conflict(new { message = ex.Message, attemptId = ex.AttemptId, detail = ex.Detail,
                businessObject = ex.BusinessObject, procedureNumber = ex.ProcedureNumber, responseXml = ex.ResponseXml });
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
