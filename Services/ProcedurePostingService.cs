using Microsoft.EntityFrameworkCore;
using Ortho_xact_api.Models;
using Ortho_xact_api.DTO;
using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace Ortho_xact_api.Services;

public class ProcedurePostingService(OrthoxactContext db, IProcedureSysproGateway gateway,
    PostingContracts contracts, ISysproOrderReader orderReader, ILogger<ProcedurePostingService> logger)
{
    public async Task<ProcedurePostingResult> Post(string order, string user)
    {
        // Must finish before login, claiming an order, or changing SYSPRO.
        try { contracts.Preflight(); }
        catch (Exception ex) { throw new InvalidOperationException("Posting configuration is incomplete: " + ex.Message, ex); }
        ProcedurePostAttempt attempt;
        List<SorHeaderStage> headers;
        List<SorDetailStage> lines;
        List<PatientProcedure> procedures;
        await using (var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
        {
            if (await db.ProcedurePostAttempts.AnyAsync(x => x.SalesOrder == order))
                throw new InvalidOperationException("This order already has a posting attempt. An administrator must inspect its outcome; it cannot be posted again.");
            headers = await db.SorHeaders.Where(x => x.SalesOrder == order).OrderBy(x => x.ProcedureNumber).ToListAsync();
            lines = await db.SorStageDetails.Where(x => x.SalesOrder == order).ToListAsync();
            if (headers.Count == 0 || headers.Any(h => !lines.Any(l => l.ProcedureNumber == h.ProcedureNumber)) ||
                !ProcedureStagingService.CanPost(lines))
                throw new InvalidOperationException("Check and validate every line in every procedure with no variances before posting.");
            procedures = await db.PatientProcedures.Where(x => x.SalesOrder == order).ToListAsync();
            if (procedures.Count != headers.Count || headers.Any(h => !procedures.Any(p => p.ProcedureNumber == h.ProcedureNumber)) ||
                procedures.Any(p => p.PostingAttemptId.HasValue || !string.IsNullOrWhiteSpace(p.ReplacementSalesOrder)))
                throw new InvalidOperationException("Procedure reporting records are missing or already linked to a posting attempt. An administrator must review this order.");
            ValidateRelationships(lines);
            foreach (var group in lines.GroupBy(x => x.ProcedureNumber))
                ProcedureStagingService.RequireProcedureParent(group.Key, group.Select(x => x.KitFlag));
            attempt = new ProcedurePostAttempt { Id = Guid.NewGuid(), SalesOrder = order, CreatedAt = DateTime.UtcNow,
                CreatedBy = user, SnapshotJson = JsonSerializer.Serialize(new { Headers = headers, Lines = lines }) };
            db.ProcedurePostAttempts.Add(attempt);
            foreach (var procedure in procedures)
            {
                procedure.PostingAttemptId = attempt.Id;
                procedure.PostingStatus = "Posting";
                procedure.PostedAt = null;
            }
            foreach (var line in lines) line.Status = "Posting";
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        string? session = null;
        try
        {
            session = await gateway.Login();
            // Build the complete plan before releasing the source for invoicing. Keep sequences as strings (including leading zeros).
            foreach (var parent in lines.Where(x => x.KitFlag == "P"))
            {
                var h = headers.Single(x => x.ProcedureNumber == parent.ProcedureNumber);
                var request = new XElement("Build", new XElement("Parameters",
                    new XElement("StockCode", parent.StockCode), new XElement("Warehouse", parent.Warehouse),
                    new XElement("Customer", h.Customer), new XElement("OrderQuantity", parent.OrderQty),
                    new XElement("ShowKitComponents", "Y")));
                var built = await Call(attempt, parent.ProcedureNumber, "SORRSL", "", request.ToString(),
                    () => gateway.Build(session, "SORRSL", request.ToString()), output => {
                        PostingContracts.CheckErrors(output);
                        if (output.Name.LocalName != "SoLine") throw new InvalidOperationException("SORRSL returned an unexpected document.");
                    });
                AssignBomSequences(parent, lines, built);
            }
            var plans = headers.Select(h => Plan(h, lines.Where(l => l.ProcedureNumber == h.ProcedureNumber).ToList())).ToList();
            // Render all request contracts before releasing the original order for invoicing.
            foreach (var plan in plans)
            {
                ValidateImportRequest(contracts.Request("SORTOI", plan), order);
                contracts.Request("SORTSU", plan);
                contracts.Request("SORTOX", plan);
            }
            var releaseInput = new XElement("Posting", new XElement("OriginalSalesOrder", order), new XElement("NewOrderStatus", "8"));
            var releaseRequest = PostingContracts.Parse(contracts.Request("SORTOS", releaseInput));
            ValidateReleaseRequest(releaseRequest, order);
            await ContractCall("SORTOS", "SORTOS", releaseInput, null);
            var createdOrders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var plan in plans)
            {
                var procedure = (int)plan.Element("Header")!.Element("ProcedureNumber")!;
                var imported = await ContractCall("SORTOI", "SORTOI", plan, procedure);
                var result = contracts.Result("SORTOI", imported);
                var newOrder = result.Element("SalesOrder")?.Value.Trim();
                if (string.IsNullOrWhiteSpace(newOrder) || ProcedureStagingService.Normalize(newOrder) == order ||
                    !createdOrders.Add(ProcedureStagingService.Normalize(newOrder)))
                    throw new InvalidOperationException("SORTOI did not identify a distinct new replacement sales order.");
                plan.SetElementValue("SalesOrder", newOrder);
                var procedureRecord = procedures.Single(p => p.ProcedureNumber == procedure);
                procedureRecord.ReplacementSalesOrder = newOrder;
                procedureRecord.PostingStatus = "Created";
                attempt.SnapshotJson = JsonSerializer.Serialize(new { Headers = headers, Lines = lines, Plans = plans.Select(x => x.ToString()).ToList() });
                await db.SaveChangesAsync();
                // Read back actual SYSPRO relationships before attempting component maintenance.
                await EntityRead(plan, procedure, false);
                var completion = PostingContracts.Parse(contracts.Request("SORTOX", plan));
                if (completion.Elements("Item").Any())
                {
                    var parameters = contracts.Parameters("SORTOX");
                    await Call(attempt, procedure, "SORTOX", parameters, completion.ToString(),
                        () => gateway.Post(session, "SORTOX", parameters, completion.ToString()),
                        response => PostingXml.VerifyCompletion(completion, response));
                    var completed = plan.Element("Lines")!.Elements("Line")
                        .Where(x => PostingXml.IsComponent(x) && PostingXml.Quantity(x) == 0).ToList();
                    foreach (var line in completed) line.SetElementValue("Completed", "Y");
                    attempt.SnapshotJson = JsonSerializer.Serialize(new { Headers = headers, Lines = lines, Plans = plans.Select(x => x.ToString()).ToList() });
                    await db.SaveChangesAsync();
                    // Completed lines remain in the order; verify all identities before maintaining positive components.
                    await EntityRead(plan, procedure, false);
                }
                if (plan.Element("Lines")!.Elements("Line").Any(x => PostingXml.IsComponent(x) && PostingXml.Quantity(x) > 0))
                    await ContractCall("SORTSU", "SORTSU", plan, procedure);
                await EntityRead(plan, procedure, true);
                procedureRecord.PostingStatus = "Completed";
                procedureRecord.PostedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
            attempt.Status = "Succeeded";
            foreach (var line in lines) line.Status = "PostedToSyspro";
            var legacy = await db.DeliveryOrderDetails.Where(x => x.SalesOrder == order).ToListAsync();
            foreach (var line in legacy) { line.Status = "PostedToSyspro"; line.PostedDate = DateTime.UtcNow; line.PostedBy = user; }
            await db.SaveChangesAsync();
            return new ProcedurePostingResult(attempt.Id, order, plans.Select(plan =>
                new CreatedSalesOrder((int)plan.Element("Header")!.Element("ProcedureNumber")!,
                    plan.Element("SalesOrder")!.Value)).ToList());

            async Task<XElement> EntityRead(XElement plan, int procedure, bool final)
            {
                var request = new XElement("EntityRead", new XElement("SalesOrder", plan.Element("SalesOrder")!.Value),
                    new XElement("VerifyQuantities", final));
                return await Call(attempt, procedure, "SYSPRO_ENTITY_READ", "", request.ToString(),
                    async () => (await orderReader.Read(plan.Element("SalesOrder")!.Value)).ToString(),
                    result => VerifyCreatedLines(plan, result, final));
            }

            async Task<XElement> ContractCall(string contract, string bo, XElement input, int? procedure)
            {
                var request = contracts.Request(contract, input);
                var parameters = contracts.Parameters(contract);
                return await Call(attempt, procedure, bo, parameters, request,
                    () => gateway.Post(session, bo, parameters, request),
                    output => contracts.CheckSuccess(contract, output));
            }
        }
        catch (Exception ex)
        {
            // A transport failure can mean the order was committed remotely. Never replay automatically.
            attempt.Status = "RequiresReview";
            foreach (var procedure in procedures.Where(p => p.PostingStatus != "Completed"))
                procedure.PostingStatus = "RequiresReview";
            foreach (var line in lines) line.Status = "SysproPostingFailed";
            db.UnsuccessfulSOMasters.Add(new UnsuccessfulSOMaster {
                Id = attempt.Id, SalesOrder = order, CreatedAt = DateTime.UtcNow, Error = ex.Message
            });
            var steps = await db.ProcedurePostSteps.Where(x => x.AttemptId == attempt.Id).ToListAsync();
            foreach (var step in steps)
                db.UnsuccessfulSODetails.Add(new UnsuccessfulSODetail { MasterId = attempt.Id,
                    ProcedureNumber = step.ProcedureNumber, BusinessObject = step.BusinessObject,
                    RequestXml = step.RequestXml, ResponseXml = step.ResponseXml });
            await db.SaveChangesAsync();
            var failedStep = steps.Where(x => x.Status == "Failed" || x.Status == "Unknown")
                .OrderByDescending(x => x.Id).FirstOrDefault();
            throw new ProcedurePostingException(
                $"Posting stopped. Reference {attempt.Id}. An administrator must review the saved SYSPRO responses before any further posting.",
                attempt.Id, failedStep != null ? ex.Message : "Posting could not complete. An administrator can check the server log.",
                failedStep?.BusinessObject, failedStep?.ProcedureNumber, failedStep?.ResponseXml ?? "", ex);
        }
        finally
        {
            if (session != null)
                try { await gateway.Logout(session); }
                catch (Exception ex) { logger.LogWarning(ex, "SYSPRO logout failed for posting attempt {Attempt}", attempt.Id); }
        }
    }

    private async Task<XElement> Call(ProcedurePostAttempt attempt, int? procedure, string bo, string parameters,
        string request, Func<Task<string>> send, Action<XElement> validate)
    {
        var step = new ProcedurePostStep { AttemptId = attempt.Id, ProcedureNumber = procedure,
            BusinessObject = bo, ParametersXml = parameters, RequestXml = request, CreatedAt = DateTime.UtcNow };
        db.ProcedurePostSteps.Add(step);
        await db.SaveChangesAsync(); // Durable intent before the remote call.
        try
        {
            step.ResponseXml = await send() ?? "";
            // Preserve raw XML even if parsing/validation subsequently fails.
            await db.SaveChangesAsync();
            var xml = PostingContracts.Parse(step.ResponseXml);
            validate(xml);
            step.Status = bo == "SORTSU" ? "ResponseChecked" : "Succeeded";
            await db.SaveChangesAsync();
            return xml;
        }
        catch
        {
            step.Status = string.IsNullOrWhiteSpace(step.ResponseXml) ? "Unknown" : "Failed";
            await db.SaveChangesAsync();
            throw;
        }
    }

    public static void ValidateRelationships(List<SorDetailStage> lines)
    {
        foreach (var line in lines)
        {
            if (line.KitFlag == "C" && !line.ParentLine.HasValue)
                throw new InvalidOperationException($"Component {line.Line} has no saved parent.");
            if (line.ParentLine.HasValue && !lines.Any(p => p.Line == line.ParentLine &&
                p.ProcedureNumber == line.ProcedureNumber && p.KitFlag == "P"))
                throw new InvalidOperationException($"Component {line.Line} is not assigned to a parent in its procedure.");
        }
    }
    public static void AssignBomSequences(SorDetailStage parent, List<SorDetailStage> lines, XElement built)
    {
        var components = built.Descendants().Where(x => x.Name.LocalName == "KitComponents")
            .SelectMany(x => x.Elements().Where(c => c.Name.LocalName == "Component")).ToList();
        var used = new HashSet<string>();
        foreach (var line in lines.Where(x => x.ParentLine == parent.Line && x.ProcedureNumber == parent.ProcedureNumber))
        {
            string Value(XElement e, string name) => e.Elements().SingleOrDefault(x => x.Name.LocalName == name)?.Value.Trim() ?? "";
            var matches = components.Where(c => Value(c, "ComponentStockCode") == line.StockCode &&
                (string.IsNullOrWhiteSpace(line.ComponentSequence) || Value(c, "ComponentSequence") == line.ComponentSequence)).ToList();
            if (matches.Count != 1)
                throw new InvalidOperationException($"SORRSL cannot uniquely match component {line.StockCode} under parent {parent.StockCode}.");
            var sequence = Value(matches[0], "ComponentSequence");
            if (!long.TryParse(sequence, NumberStyles.None, CultureInfo.InvariantCulture, out _) ||
                !used.Add(line.StockCode + ":" + sequence))
                throw new InvalidOperationException("SORRSL returned a missing, invalid, or duplicate component identity.");
            line.ComponentSequence = sequence;
        }
        // Prevent unreviewed BOM components being automatically added to the replacement order.
        if (components.Count != used.Count)
            throw new InvalidOperationException($"The BOM for {parent.StockCode} differs from the checked order. Reconcile its components before posting.");
    }
    public static XElement Plan(SorHeaderStage header, List<SorDetailStage> lines)
    {
        ValidateRelationships(lines);
        var ordered = new List<SorDetailStage>();
        foreach (var parent in lines.Where(x => !x.ParentLine.HasValue).OrderBy(x => x.StockCode, StringComparer.Ordinal).ThenBy(x => x.Line))
        {
            ordered.Add(parent);
            ordered.AddRange(lines.Where(x => x.ParentLine == parent.Line && x.ProcedureNumber == parent.ProcedureNumber)
                .OrderBy(x => long.Parse(x.ComponentSequence, CultureInfo.InvariantCulture)).ThenBy(x => x.Line));
        }
        if (ordered.Count != lines.Count) throw new InvalidOperationException("Not all checked lines could be sequenced.");
        var positions = ordered.Select((line, i) => (line.Line, Number: i + 1)).ToDictionary(x => x.Line, x => x.Number);
        return new XElement("Posting", new XElement("OriginalSalesOrder", header.SalesOrder),
            new XElement("SalesOrder", ""), JsonFields("SourceHeader", header.SourceHeaderJson), new XElement("Header",
                new XElement("ProcedureNumber", header.ProcedureNumber), new XElement("Customer", header.Customer),
                new XElement("PatientNumber", header.PatientNumber), new XElement("PatientName", header.PatientName),
                new XElement("SurgeonName", header.SurgeonName),
                new XElement("ProcedureDate", header.ProcedureDate == default ? "" : header.ProcedureDate.ToString("yyyy-MM-dd")),
                new XElement("CgSetNumber", header.CgSetNumber)),
            new XElement("Lines", ordered.Select(line => new XElement("Line",
                JsonFields("SourceValues", line.SourceLineJson), new XElement("SourceLine", line.Line), new XElement("LineNumber", positions[line.Line]),
                new XElement("ParentLine", line.ParentLine.HasValue ? positions[line.ParentLine.Value] : (int?)null),
                new XElement("KitFlag", line.KitFlag), new XElement("ComponentSequence", line.ComponentSequence),
                new XElement("StockCode", line.StockCode), new XElement("Warehouse", line.Warehouse),
                new XElement("Bin", line.Bin), new XElement("Description", line.Description),
                new XElement("Quantity", line.KitFlag == "P" ? line.OrderQty : line.UsageQty)))));
    }
    private static XElement JsonFields(string name, string json)
    {
        using var doc = JsonDocument.Parse(json);
        return new XElement(name, doc.RootElement.EnumerateObject().Select(x => new XElement(x.Name,
            x.Value.ValueKind == JsonValueKind.Null ? "" : x.Value.ToString())));
    }
    public static void ValidateReleaseRequest(XElement request, string originalOrder)
    {
        var items = request.Elements("Item").ToList();
        if (request.Name != "PostSorOrderStatus" || items.Count != 1 ||
            items[0].Elements("SalesOrder").Count() != 1 || items[0].Elements("NewOrderStatus").Count() != 1 ||
            ProcedureStagingService.Normalize(items[0].Element("SalesOrder")!.Value) != originalOrder ||
            items[0].Element("NewOrderStatus")!.Value != "8")
            throw new InvalidOperationException("SORTOS must release exactly the original order for invoicing with status 8.");
    }
    public static void ValidateImportRequest(string xml, string originalOrder)
    {
        var request = PostingContracts.Parse(xml);
        var headers = request.Descendants("OrderHeader").ToList();
        if (headers.Count != 1 || headers[0].Element("OrderActionType")?.Value != "A" ||
            headers[0].Elements("SalesOrder").Any(x => ProcedureStagingService.Normalize(x.Value) == originalOrder))
            throw new InvalidOperationException("SORTOI must add one new order; modifying or reusing the original order is forbidden.");
        foreach (var line in request.Descendants("StockLine"))
        {
            var quantities = line.Elements("OrderQty").ToList();
            if (quantities.Count != 1 ||
                !decimal.TryParse(quantities[0].Value.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var quantity) || quantity < 0)
                throw new InvalidOperationException("SORTOI StockLine " + (string?)line.Element("StockCode") +
                    " (line " + (string?)line.Element("CustomerPoLine") +
                    ") must contain exactly one numeric, non-negative OrderQty. Check the deployed SORTOI.request.xslt override.");
        }
    }
    public static void VerifyCreatedLines(XElement plan, XElement result, bool verifyQuantities = false)
    {
        if (ProcedureStagingService.Normalize(result.Element("SalesOrder")?.Value ?? "") !=
            ProcedureStagingService.Normalize(plan.Element("SalesOrder")?.Value ?? ""))
            throw new InvalidOperationException("The queried replacement order does not match the imported order.");
        var expectedCustomer = (string?)plan.Element("Header")?.Element("Customer");
        if (expectedCustomer != null && expectedCustomer.Trim() != ((string?)result.Element("Customer"))?.Trim())
            throw new InvalidOperationException("The queried replacement customer does not match the checked plan.");
        var actual = result.Element("Lines")?.Elements("Line").ToList() ?? [];
        var expected = plan.Element("Lines")!.Elements("Line").ToList();
        if (actual.Count != expected.Count) throw new InvalidOperationException("Replacement line count differs from the checked plan.");
        foreach (var line in expected)
        {
            var match = actual.Where(x => (int?)x.Element("LineNumber") == (int?)line.Element("LineNumber")).ToList();
            if (match.Count != 1 || (string?)match[0].Element("StockCode") != (string?)line.Element("StockCode") ||
                (string?)match[0].Element("ParentLine") != (string?)line.Element("ParentLine") ||
                (!string.IsNullOrEmpty((string?)line.Element("ParentLine")) &&
                 (string?)match[0].Element("ComponentSequence") != (string?)line.Element("ComponentSequence")))
                throw new InvalidOperationException("SYSPRO kit relationships or line sequencing differ from the checked plan.");
            if (verifyQuantities && (string?)match[0].Element("Warehouse") != (string?)line.Element("Warehouse"))
                throw new InvalidOperationException("SYSPRO final warehouse differs from the checked plan.");
            // Completion is confirmed by SORTOX. Retained completed rows can keep their original order quantity.
            if (verifyQuantities && (string?)line.Element("Completed") != "Y" &&
                (!decimal.TryParse((string?)match[0].Element("Quantity"), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var actualQuantity) ||
                 !decimal.TryParse((string?)line.Element("Quantity"), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var expectedQuantity) ||
                 actualQuantity != expectedQuantity ||
                 (string?)match[0].Element("Warehouse") != (string?)line.Element("Warehouse")))
                throw new InvalidOperationException("SYSPRO final quantities or warehouses differ from the checked plan.");
        }
    }
}
