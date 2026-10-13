using System.Xml.Linq;

namespace Ortho_xact_api.Services;

public static class PostingXml
{
    public static XElement ReleaseOriginalForInvoicing(XElement input) => new("PostSorOrderStatus",
        new XElement("Item", new XElement("SalesOrder", input.Element("OriginalSalesOrder")!.Value),
            new XElement("NewOrderStatus", "8")));

    // SORTSU changes an EXISTING component line, identified after entity verification.
    // ParentLine, ShipQty and ComponentSequence are not invented SORTSU input elements.
    public static XElement ChangeComponents(XElement plan) => new("PostChangeSalesOrderKitComp",
        plan.Element("Lines")!.Elements("Line")
            .Where(x => IsComponent(x) && Quantity(x) > 0)
            .Select(line => new XElement("Item",
                new XElement("SalesOrder", plan.Element("SalesOrder")!.Value),
                new XElement("SalesOrderLine", line.Element("LineNumber")!.Value),
                new XElement("Customer", plan.Element("Header")!.Element("Customer")!.Value),
                new XElement("StockCode", line.Element("StockCode")!.Value),
                new XElement("OrderQty", line.Element("Quantity")!.Value),
                new XElement("QuantityType", "T"))));

    public static bool IsComponent(XElement line) => !string.IsNullOrWhiteSpace((string?)line.Element("ParentLine"));
    public static decimal Quantity(XElement line)
    {
        if (!decimal.TryParse((string?)line.Element("Quantity"), System.Globalization.NumberStyles.AllowDecimalPoint | System.Globalization.NumberStyles.AllowLeadingSign,
            System.Globalization.CultureInfo.InvariantCulture, out var value) || value < 0)
            throw new InvalidOperationException("Component quantity must be numeric and non-negative.");
        return value;
    }
    public static XElement CompleteZeroComponents(XElement plan) => new("PostSalesOrderCancel",
        plan.Element("Lines")!.Elements("Line").Where(x => IsComponent(x) && Quantity(x) == 0)
            .Select(line => new XElement("Item",
                new XElement("SalesOrder", plan.Element("SalesOrder")!.Value),
                new XElement("SalesOrderLine", line.Element("LineNumber")!.Value),
                new XElement("MarkLineComplete", "Y"),
                new XElement("CancelReasonCode"), new XElement("eSignature"))));

    public static void VerifyCompletion(XElement request, XElement response)
    {
        PostingContracts.CheckErrors(response);
        var expected = request.Elements("Item").ToList();
        var actual = response.Elements("Item").ToList();
        if (!response.Name.LocalName.Equals("postsalesordercancel", StringComparison.OrdinalIgnoreCase) ||
            expected.Count == 0 || actual.Count != expected.Count ||
            response.Descendants("ErrorNumber").Any(x => !long.TryParse(x.Value.Trim(), out var n) || n != 0))
            throw new InvalidOperationException("SORTOX did not confirm every component completion.");
        foreach (var item in expected)
        {
            var matches = actual.Where(x =>
                ProcedureStagingService.Normalize((string?)x.Element("SalesOrder") ?? "") ==
                ProcedureStagingService.Normalize((string?)item.Element("SalesOrder") ?? "") &&
                int.TryParse((string?)x.Element("SalesOrderLine"), out var n) &&
                n == (int)item.Element("SalesOrderLine")!).ToList();
            if (matches.Count != 1 || matches[0].Element("Completed")?.Value.Trim() != "Y" ||
                matches[0].Element("Cancelled")?.Value.Trim() != "N")
                throw new InvalidOperationException("SORTOX did not confirm completion of the requested component line.");
        }
    }

    public static XElement Parameters(string step) => step switch {
        "SORTOS" => new XElement("PostSorOrderStatus", new XElement("Parameters",
            new XElement("ValidateOnly", "N"), new XElement("IgnoreWarnings", "N"),
            new XElement("ApplyIfEntireDocumentValid", "Y"))),
        "SORTOX" => new XElement("PostSalesOrderCancel", new XElement("Parameters", new XElement("IgnoreWarnings", "N"), new XElement("ApplyIfEntireDocumentValid", "Y"), new XElement("ValidateOnly", "N"), new XElement("CancelAttachedComments", "N"))),
        "SORTSU" => new XElement("PostChangeSalesOrderKitComp", new XElement("Parameters",
            new XElement("IgnoreWarnings", "N"), new XElement("ApplyIfEntireDocumentValid", "Y"),
            new XElement("ValidateOnly", "N"))),
        _ => throw new InvalidOperationException("No built-in parameter contract for " + step)
    };
}
