using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ortho_xact_api.DTO;
using Ortho_xact_api.Models;
using Ortho_xact_api.Services;
using System.Xml.Linq;

namespace Ortho_xact_api.Controllers;

[ApiController]
[Route("Rep")]
[Authorize(Roles = "admin")]
public class SortoiTrialController(OrthoxactContext db, IProcedureSysproGateway gateway,
    PostingContracts contracts, ILogger<SortoiTrialController> logger) : ControllerBase
{
    [HttpPost("validate-sortoi-components")]
    public async Task<IActionResult> ValidateComponents([FromBody] SalesOrderRequest request)
    {
        var order = ProcedureStagingService.Normalize(request.SalesOrderNumber);
        if (string.IsNullOrEmpty(order)) return BadRequest(new { message = "A sales order is required." });
        var headers = await db.SorHeaders.AsNoTracking().Where(x => x.SalesOrder == order)
            .OrderBy(x => x.ProcedureNumber).ToListAsync();
        var lines = await db.SorStageDetails.AsNoTracking().Where(x => x.SalesOrder == order).ToListAsync();
        if (headers.Count == 0 || lines.Count == 0)
            return NotFound(new { message = "No staged procedure data exists for this order." });
        string? session = null;
        try
        {
            contracts.Preflight();
            ProcedurePostingService.ValidateRelationships(lines);
            session = await gateway.Login();
            foreach (var parent in lines.Where(x => x.KitFlag == "P"))
            {
                var header = headers.Single(x => x.ProcedureNumber == parent.ProcedureNumber);
                var build = new XElement("Build", new XElement("Parameters",
                    new XElement("StockCode", parent.StockCode), new XElement("Warehouse", parent.Warehouse),
                    new XElement("Customer", header.Customer), new XElement("OrderQuantity", parent.OrderQty),
                    new XElement("ShowKitComponents", "Y")));
                var output = PostingContracts.Parse(await gateway.Build(session, "SORRSL", build.ToString()));
                PostingContracts.CheckErrors(output);
                if (output.Name.LocalName != "SoLine")
                    throw new InvalidOperationException("SORRSL returned an unexpected document.");
                ProcedurePostingService.AssignBomSequences(parent, lines, output);
            }
            var results = new List<object>();
            foreach (var header in headers)
            {
                var plan = ProcedurePostingService.Plan(header,
                    lines.Where(x => x.ProcedureNumber == header.ProcedureNumber).ToList());
                var trial = SortoiMapping.ComponentValidationTrial(plan);
                // Respect configured pricing flags, but never permit this route to import.
                var parameters = PostingContracts.Parse(contracts.Parameters("SORTOI"));
                parameters.Element("Parameters")!.SetElementValue("Process", "Validate");
                var response = await gateway.Post(session, "SORTOI", parameters.ToString(), trial.Request.ToString());
                results.Add(new { header.ProcedureNumber, requestXml = trial.Request.ToString(),
                    parametersXml = parameters.ToString(), responseXml = response });
            }
            return Ok(new { mode = "Validate", message = "Validation trial only. No order was imported or released. Explicit child lines do not prove native kit relationships.", results });
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        finally
        {
            if (session != null)
                try { await gateway.Logout(session); }
                catch { logger.LogWarning("SYSPRO logout failed after component validation trial."); }
        }
    }
}
