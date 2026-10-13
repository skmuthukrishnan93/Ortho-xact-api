using Microsoft.EntityFrameworkCore;
using Ortho_xact_api.SysModels;
using System.Xml.Linq;

namespace Ortho_xact_api.Services;

public interface ISysproOrderReader
{
    Task<XElement> Read(string salesOrder);
}

public record SysproReadLine(decimal Number, string StockCode, string Warehouse,
    decimal Quantity, string KitFlag, string ComponentSequence);
public record SysproReadKitReference(decimal Number, string StockCode, string SetCode);

// Read-only replacement for SORQRY. Uses the same source set identity as procedure staging.
public class SysproEntityOrderReader(SysproContext sys) : ISysproOrderReader
{
    public async Task<XElement> Read(string salesOrder)
    {
        var order = ProcedureStagingService.Normalize(salesOrder);
        if (order.Length == 0) throw new InvalidOperationException("A replacement sales order is required for entity readback.");
        // Keep the short readback consistent; no database writes or remote calls occur in this transaction.
        await using var transaction = await sys.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var header = await sys.SorMasters.AsNoTracking().Where(x => x.SalesOrder == order)
            .Select(x => new { x.SalesOrder, x.Customer }).SingleOrDefaultAsync()
            ?? throw new InvalidOperationException($"Replacement sales order {order} was not found in the configured SYSPRO database.");
        var lines = await sys.SorDetails.AsNoTracking().Where(x => x.SalesOrder == order)
            .OrderBy(x => x.SalesOrderLine)
            .Select(x => new SysproReadLine(x.SalesOrderLine, x.MstockCode, x.Mwarehouse,
                x.MorderQty, x.MbomFlag, x.McomponentSeq)).ToListAsync();
        // Do not filter to warehouse status 4: replacements are normally created at status 1.
        var references = lines.Any(x => (x.KitFlag ?? "").Trim() == "C")
            ? await sys.VwFetchSordetails.AsNoTracking().Where(x => x.SalesOrder == order)
                .Select(x => new SysproReadKitReference(x.SalesOrderLine, x.MstockCode, x.SetsCode)).ToListAsync()
            : new List<SysproReadKitReference>();
        var result = Map(header.SalesOrder, header.Customer, lines, references);
        await transaction.CommitAsync();
        return result;
    }

    public static XElement Map(string order, string customer, IReadOnlyList<SysproReadLine> lines,
        IReadOnlyList<SysproReadKitReference> references)
    {
        if (string.IsNullOrWhiteSpace(order) || lines.Count == 0 ||
            lines.Any(x => x.Number < 1 || x.Number > int.MaxValue || decimal.Truncate(x.Number) != x.Number) ||
            lines.Select(x => x.Number).Distinct().Count() != lines.Count)
            throw new InvalidOperationException("SYSPRO entity readback returned missing or invalid order lines.");
        static string Clean(string? value) => value?.Trim() ?? "";
        var resultLines = new List<XElement>();
        foreach (var line in lines.OrderBy(x => x.Number))
        {
            int? parentLine = null;
            if (Clean(line.KitFlag) == "C")
            {
                var matches = references.Where(x => x.Number == line.Number).ToList();
                if (matches.Count != 1 || Clean(matches[0].StockCode) != Clean(line.StockCode) ||
                    Clean(matches[0].SetCode).Length == 0)
                    throw new InvalidOperationException($"Component line {line.Number} has no unique source set identity in vw_FetchSORDetails.");
                var parents = lines.Where(x => Clean(x.KitFlag) == "P" &&
                    Clean(x.StockCode) == Clean(matches[0].SetCode)).ToList();
                if (parents.Count != 1)
                    throw new InvalidOperationException($"Component line {line.Number} has no unique kit parent in the replacement order.");
                parentLine = (int)parents[0].Number;
                if (Clean(line.ComponentSequence).Length == 0)
                    throw new InvalidOperationException($"Component line {line.Number} has no stored SYSPRO component sequence.");
            }
            resultLines.Add(new XElement("Line", new XElement("LineNumber", (int)line.Number),
                new XElement("StockCode", Clean(line.StockCode)), new XElement("ParentLine", parentLine),
                new XElement("ComponentSequence", Clean(line.ComponentSequence)),
                new XElement("Warehouse", Clean(line.Warehouse)), new XElement("Quantity", line.Quantity)));
        }
        return new XElement("Result", new XElement("SalesOrder", order.Trim()),
            new XElement("Customer", Clean(customer)), new XElement("Lines", resultLines));
    }
}
