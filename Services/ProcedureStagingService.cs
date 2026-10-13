using Microsoft.EntityFrameworkCore;
using Ortho_xact_api.Models;
using Ortho_xact_api.SysModels;
using System.Text.Json;

namespace Ortho_xact_api.Services;

public class ProcedureStagingService(OrthoxactContext db, SysproContext sys)
{
    public static string Normalize(string value) => long.TryParse(value?.Trim(), out var n) ? n.ToString("D15") : value?.Trim() ?? "";

    public static void RequireProcedureParent(int procedureNumber, IEnumerable<string> kitFlags)
    {
        if (!kitFlags.Any(flag => string.Equals(flag?.Trim(), "P", StringComparison.Ordinal)))
            throw new InvalidOperationException($"Procedure {procedureNumber} must contain at least one kit parent.");
    }

    public sealed record SourceSnapshot(string SalesOrder, SorMaster Master,
        List<SorDetail> Lines, List<VwFetchSordetail> View);

    // Read the separate connection before taking application write locks. The view may join app tables.
    public async Task<SourceSnapshot> LoadSource(string order)
    {
        var master = await sys.SorMasters.AsNoTracking().SingleAsync(x => x.SalesOrder == order);
        var lines = await sys.SorDetails.AsNoTracking().Where(x => x.SalesOrder == order).ToListAsync();
        var view = await sys.VwFetchSordetails.AsNoTracking().Where(x => x.SalesOrder == order).ToListAsync();
        return new SourceSnapshot(order, master, lines, view);
    }

    // Runs inside the Rep save transaction. Warehouse reads use only app tables.
    public async Task Capture(string order, SourceSnapshot snapshot)
    {
        if (await db.ProcedurePostAttempts.AnyAsync(x => x.SalesOrder == order))
            throw new InvalidOperationException("Posting has already started. An administrator must reconcile this order before it can be changed.");
        var saved = await db.DeliveryOrderDetails.Where(x => x.SalesOrder == order).ToListAsync();
        var warehouseStatuses = new[] { "RepCompleted", "StoresInProgress", "Completed&ReadyForValidation",
            "ReadyForValidate", "ReadyToPostSyspro", "Send Email To Customer Service" };
        if (saved.Count == 0 || saved.Any(x => !warehouseStatuses.Contains(x.Status)))
            throw new InvalidOperationException("This order is not ready for warehouse review.");
        var procedures = await db.PatientProcedures.Where(x => x.SalesOrder == order).OrderBy(x => x.ProcedureNumber).ToListAsync();
        if (procedures.Count == 0)
            procedures.Add(new PatientProcedure { SalesOrder = order, ProcedureNumber = 1,
                LineNumbersJson = JsonSerializer.Serialize(saved.Select(x => x.Line)) });
        if (snapshot.SalesOrder != order) throw new InvalidOperationException("Source snapshot belongs to another order.");
        var master = snapshot.Master;
        var source = snapshot.Lines;
        var view = snapshot.View;
        if (saved.Count == 0 || !saved.Select(x => x.Line).ToHashSet().SetEquals(source.Select(x => (int)x.SalesOrderLine)))
            throw new InvalidOperationException("The saved capture does not contain the complete source order. Reopen it in Rep capture.");
        var assigned = procedures.SelectMany(p => (JsonSerializer.Deserialize<List<int>>(p.LineNumbersJson) ?? [])
            .Select(line => (line, p.ProcedureNumber))).ToList();
        if (assigned.Select(x => (x.line, x.ProcedureNumber)).Distinct().Count() != assigned.Count ||
            !assigned.Select(x => x.line).ToHashSet().SetEquals(saved.Select(x => x.Line)))
            throw new InvalidOperationException("Assign every line to exactly one procedure before warehouse review.");
        foreach (var group in assigned.GroupBy(x => x.ProcedureNumber))
            RequireProcedureParent(group.Key, source.Where(x => group.Any(a => a.line == (int)x.SalesOrderLine)).Select(x => x.MbomFlag));
        db.SorStageDetails.RemoveRange(await db.SorStageDetails.Where(x => x.SalesOrder == order).ToListAsync());
        await db.SaveChangesAsync();
        db.SorHeaders.RemoveRange(await db.SorHeaders.Where(x => x.SalesOrder == order).ToListAsync());
        await db.SaveChangesAsync();
        foreach (var p in procedures)
            db.SorHeaders.Add(new SorHeaderStage { SalesOrder = order, ProcedureNumber = p.ProcedureNumber,
                PatientNumber = p.PatientNumber, PatientName = p.PatientName, SurgeonName = p.SurgeonName,
                ProcedureDate = p.ProcedureDate, CgSetNumber = p.CgSetNumber,
                Customer = master.Customer, SourceHeaderJson = JsonSerializer.Serialize(master) });
        foreach (var assignment in assigned)
        {
            var savedLine = saved.Single(x => x.Line == assignment.line);
            var src = source.Single(x => x.SalesOrderLine == savedLine.Line);
            var v = view.Single(x => x.SalesOrderLine == savedLine.Line);
            var procedure = assignment.ProcedureNumber;
            var quantities = JsonSerializer.Deserialize<Dictionary<int, decimal>>(procedures.Single(p => p.ProcedureNumber == procedure).LineQuantitiesJson) ?? new();
            var repUsage = quantities.TryGetValue(savedLine.Line, out var capturedUsage) ? capturedUsage : savedLine.RepUsageQty ?? 0;
            if (src.MshipQty < 0 || repUsage < 0)
                throw new InvalidOperationException($"Line {savedLine.Line} has invalid saved usage quantities.");
            int? parent = null;
            if (src.MbomFlag.Trim() == "C")
            {
                // Match a source set identity, never adjacency or an invented ParentLine=1.
                var candidates = source.Where(x => x.MbomFlag.Trim() == "P" &&
                    x.MstockCode.Trim() == v.SetsCode.Trim() &&
                    assigned.Any(a => a.line == (int)x.SalesOrderLine && a.ProcedureNumber == procedure)).ToList();
                if (candidates.Count != 1)
                    throw new InvalidOperationException($"Line {savedLine.Line} has no unambiguous kit parent in procedure {procedure}. Correct the source assignment.");
                parent = (int)candidates[0].SalesOrderLine;
            }
            db.SorStageDetails.Add(new SorDetailStage { SalesOrder = order, Line = savedLine.Line,
                ProcedureNumber = procedure, ParentLine = parent, KitFlag = src.MbomFlag.Trim(),
                SourceLineJson = JsonSerializer.Serialize(src), SetCode = v.SetsCode.Trim(),
                ComponentSequence = src.McomponentSeq.Trim(), StockCode = src.MstockCode.Trim(),
                Warehouse = src.Mwarehouse.Trim(), Bin = src.Mbin.Trim(), Description = src.MstockDes,
                DeliveredQty = src.MshipQty, OrderQty = src.MorderQty, RepUsageQty = repUsage,
                Status = "RepCompleted" });
        }
        await db.SaveChangesAsync();
    }

    public async Task EnsureStaged(string order)
    {
        if (await db.SorHeaders.AnyAsync(x => x.SalesOrder == order)) return;
        // One-time migration of older saved captures, requiring a new warehouse check.
        var snapshot = await LoadSource(order);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (!await db.SorHeaders.AnyAsync(x => x.SalesOrder == order)) await Capture(order, snapshot);
        await tx.CommitAsync();
    }

    public async Task ResetForCorrection(string order, bool clerk)
    {
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        if (await db.ProcedurePostAttempts.AnyAsync(x => x.SalesOrder == order))
            throw new InvalidOperationException("This order has a posting attempt and must be reconciled by an administrator; correction cannot unlock it.");
        var status = clerk ? "StoresInProgress" : "Inprogress";
        var staged = await db.SorStageDetails.Where(x => x.SalesOrder == order).ToListAsync();
        foreach (var line in staged) { line.Status = status; line.CheckedAt = null; }
        var legacy = await db.DeliveryOrderDetails.Where(x => x.SalesOrder == order).ToListAsync();
        foreach (var line in legacy) line.Status = status;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public static bool IsChecked(SorDetailStage line) => line.KitFlag == "P" ||
        (line.CheckedAt.HasValue && line.ReturnQty.HasValue && line.ReturnQty >= 0 && line.ReturnQty <= line.DeliveredQty);
    public static bool CanPost(IEnumerable<SorDetailStage> lines)
    {
        var all = lines.ToList();
        return all.Count > 0 && all.All(x => x.Status == "ReadyToPostSyspro" && IsChecked(x) &&
            (x.KitFlag == "P" || (x.Variance == 0 && x.UsageQty == x.DeliveredQty - x.ReturnQty && x.RepUsageQty == x.UsageQty)));
    }
}
