using System.Globalization;
using System.Xml.Linq;

namespace Ortho_xact_api.Services;

// Standard SORTOI mapping from the persisted application staging snapshot.
// Company-specific XSLT can still override this mapping when needed.
public static class SortoiMapping
{
    private static string Value(XElement? node, string name) => node?.Element(name)?.Value.Trim() ?? "";
    private static string Required(XElement? node, string name)
    {
        var value = Value(node, name);
        if (value.Length == 0) throw new InvalidOperationException("SORTOI requires a saved " + name + ".");
        return value;
    }
    private static XElement? Optional(XElement? source, string sourceName, string targetName)
    {
        var value = Value(source, sourceName);
        return value.Length == 0 ? null : new XElement(targetName, value);
    }
    private static XElement? Date(XElement? source, string sourceName, string targetName)
    {
        var value = Value(source, sourceName);
        if (value.Length == 0) return null;
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            throw new InvalidOperationException("Invalid saved date for SORTOI: " + sourceName);
        return new XElement(targetName, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
    private static string PriceUnit(XElement line)
    {
        var values = line.Element("SourceValues");
        var unit = Value(values, "MpriceUom");
        if (unit.Length > 0) return unit;
        // Zero remains zero in the saved order unit; do not guess the basis of a nonzero price.
        if (decimal.TryParse(Value(values, "Mprice"), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out var price) && price == 0)
        {
            var orderUnit = Value(values, "MorderUom");
            if (orderUnit.Length > 0) return orderUnit;
        }
        throw new InvalidOperationException("SORTOI requires a saved price unit for stock " +
            Value(line, "StockCode") + ", source line " + Value(line, "SourceLine") +
            " (posting line " + Value(line, "LineNumber") +
            "). Correct the source price unit and recapture; nonzero prices cannot use a guessed unit.");
    }

    public static XElement Request(XElement plan) => Request(plan, false);

    // Trial only: explicit children may duplicate automatic kit expansion.
    public static (XElement Request, XElement Parameters) ComponentValidationTrial(XElement plan)
    {
        var parameters = Parameters();
        parameters.Element("Parameters")!.SetElementValue("Process", "Validate");
        return (Request(plan, true), parameters);
    }

    private static XElement Request(XElement plan, bool includeChildren)
    {
        var header = plan.Element("Header");
        var source = plan.Element("SourceHeader");
        var orderHeader = new XElement("OrderHeader",
            Optional(source, "CustomerPoNumber", "CustomerPoNumber"),
            new XElement("OrderActionType", "A"),
            new XElement("Customer", Required(header, "Customer")),
            Date(source, "OrderDate", "OrderDate"),
            Optional(source, "InvTermsOverride", "InvoiceTerms"),
            Optional(source, "Currency", "Currency"),
            Optional(source, "ShippingInstrs", "ShippingInstrs"),
            Optional(source, "ShippingInstrsCod", "ShippingInstrsCode"),
            Optional(source, "ShipAddress1", "ShipAddress1"),
            Optional(source, "ShipAddress2", "ShipAddress2"),
            Optional(source, "ShipAddress3", "ShipAddress3"),
            Optional(source, "ShipAddress3Loc", "ShipAddress3Locality"),
            Optional(source, "ShipAddress4", "ShipAddress4"),
            Optional(source, "ShipAddress5", "ShipAddress5"),
            Optional(source, "ShipPostalCode", "ShipPostalCode"),
            Optional(source, "ShipToGpsLat", "ShipGpsLat"),
            Optional(source, "ShipToGpsLong", "ShipGpsLong"),
            Optional(source, "DiscPct1", "OrderDiscPercent1"),
            Optional(source, "DiscPct2", "OrderDiscPercent2"),
            Optional(source, "DiscPct3", "OrderDiscPercent3"),
            Optional(source, "Warehouse", "Warehouse"),
            Optional(source, "SpecialInstrs", "SpecialInstrs"),
            // Never copy the original SalesOrder. Automatic numbering allocates the replacement.
            new XElement("SalesOrder"),
            Optional(source, "OrderType", "OrderType"),
            Optional(source, "MultiShipCode", "MultiShipCode"),
            Optional(source, "Salesperson", "Salesperson"),
            Optional(source, "Branch", "Branch"),
            Optional(source, "Area", "Area"),
            Date(source, "ReqShipDate", "RequestedShipDate"),
            Optional(source, "Nationality", "Nationality"),
            Optional(source, "DeliveryTerms", "DeliveryTerms"),
            Optional(source, "ShippingLocation", "ShippingLocation"),
            Optional(source, "TaxExemptNumber", "TaxExemptNumber"),
            Optional(source, "TaxExemptFlag", "TaxExemptionStatus"),
            Optional(source, "GstExemptNum", "GstExemptNumber"),
            Optional(source, "GstExemptFlag", "GstExemptionStatus"));

        var details = new XElement("OrderDetails");
        var lines = plan.Element("Lines")?.Elements("Line").ToList() ?? [];
        if (lines.Count == 0) throw new InvalidOperationException("SORTOI requires saved order lines.");
        foreach (var line in lines)
        {
            // Normal import sends kit parents only; SYSPRO expands the BOM.
            if (!includeChildren && (Value(line, "KitFlag") != "P" || Value(line, "ParentLine").Length > 0)) continue;
            if (Value(line, "KitFlag") == "C" && Value(line, "ParentLine").Length == 0)
                throw new InvalidOperationException("SORTOI component has no parent assignment.");
            var values = line.Element("SourceValues");
            if (includeChildren)
            {
                var price = Required(values, "Mprice");
                if (!decimal.TryParse(price, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out _))
                    throw new InvalidOperationException("Invalid saved price for " + Required(line, "StockCode") + ".");
                PriceUnit(line);
            }
            if (Value(values, "LineType") is not ("" or "1"))
                throw new InvalidOperationException("The built-in SORTOI mapping accepts stocked merchandise only. This order requires a mapping for its non-merchandise lines.");
            var quantityText = Required(line, "Quantity");
            if (!decimal.TryParse(quantityText, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var quantity) || quantity < 0 ||
                (Value(line, "KitFlag") == "P" && quantity == 0))
                throw new InvalidOperationException("SORTOI received an invalid checked quantity.");
            details.Add(new XElement("StockLine",
                new XElement("CustomerPoLine", Required(line, "LineNumber")),
                new XElement("LineActionType", "A"),
                new XElement("StockCode", Required(line, "StockCode")),
                Optional(line, "Description", "StockDescription"),
                new XElement("Warehouse", Required(line, "Warehouse")),
                Optional(values, "McusSupStkCode", "CustomersPartNumber"),
                new XElement("OrderQty", quantity),
                new XElement("OrderUom", Required(values, "MorderUom")),
                Optional(values, "Mprice", "Price"),
                new XElement("PriceUom", PriceUnit(line)),
                Optional(values, "MproductClass", "ProductClass"),
                Optional(values, "MdiscPct1", "LineDiscPercent1"),
                Optional(values, "MdiscPct2", "LineDiscPercent2"),
                Optional(values, "MdiscPct3", "LineDiscPercent3"),
                new XElement("AlwaysUseDiscountEntered", "Y"),
                Optional(values, "McommissionCode", "CommissionCode"),
                Date(values, "MlineShipDate", "LineShipDate"),
                Optional(values, "MdiscValue", "LineDiscValue"),
                Optional(values, "MdiscValFlag", "LineDiscValFlag"),
                Optional(values, "MtaxCode", "StockTaxCode"),
                Optional(values, "MfstTaxCode", "StockFstCode")));
        }
        if (!details.HasElements) throw new InvalidOperationException("SORTOI has no kit parent lines to create.");
        return new XElement("SalesOrders", new XElement("Orders", orderHeader, details));
    }

    public static XElement Parameters() => new("SalesOrders", new XElement("Parameters",
        new XElement("Process", "Import"),
        new XElement("TypeOfOrder", "ORD"),
        new XElement("OrderStatus", "1"),
        new XElement("MinimumDaysToShip", "0"),
        new XElement("AcceptEarlierShipDate", "Y"),
        new XElement("AllowDuplicateOrderNumbers", "A"),
        new XElement("CheckForCustomerPoNumbers", "N"),
        new XElement("AlwaysUsePriceEntered", "Y"),
        new XElement("AllowZeroPrice", "Y"),
        new XElement("AllowChangeToZeroPrice", "Y"),
        new XElement("AddStockSalesOrderText", "N"),
        new XElement("AddDangerousGoodsText", "N"),
        new XElement("UseStockDescSupplied", "Y"),
        new XElement("IgnoreWarnings", "Y"),
        new XElement("AcceptKitOptional", "Y")));

    public static XElement Result(XElement response)
    {
        PostingContracts.CheckErrors(response);
        if (response.Name.LocalName != "SalesOrders")
            throw new InvalidOperationException("SORTOI returned an unexpected response.");
        var orders = response.Elements().Where(x => x.Name.LocalName == "Order").ToList();
        var status = response.Elements().SingleOrDefault(x => x.Name.LocalName == "StatusOfItems");
        string Field(XElement? node, string name) => node?.Elements().SingleOrDefault(x => x.Name.LocalName == name)?.Value.Trim() ?? "";
        if (orders.Count != 1 || Field(orders[0], "OrderActionType") != "A" ||
            Field(orders[0], "SalesOrder").Length == 0 ||
            !int.TryParse(Field(status, "ItemsProcessed"), out var processed) || processed != 1)
            throw new InvalidOperationException("SORTOI did not confirm that exactly one new order was created.");
        var rejected = Field(status, "ItemsRejectedWithWarnings");
        if (rejected.Length > 0 && (!int.TryParse(rejected, out var count) || count != 0))
            throw new InvalidOperationException("SORTOI rejected the order with warnings.");
        return new XElement("Result", new XElement("SalesOrder", Field(orders[0], "SalesOrder")));
    }
}
