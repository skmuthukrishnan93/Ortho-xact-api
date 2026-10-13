using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace Ortho_xact_api.Services;

// LiquidERP supplies version/company-specific mappings. No live posting without all contracts.
public class PostingContracts(IConfiguration config)
{
    private readonly Dictionary<string, XslCompiledTransform> transforms = new();
    private readonly Dictionary<string, string> parameters = new();
    private readonly Dictionary<string, string> success = new();
    public void Preflight()
    {
        if (!config.GetValue<bool>("SysproPosting:Enabled"))
            throw new InvalidOperationException("SYSPRO replacement posting is not enabled. LiquidERP must approve and configure the posting contracts first.");
        foreach (var name in new[] { "ServiceUrl", "Operator", "Company", "ContractsDirectory" })
            if (string.IsNullOrWhiteSpace(config["SysproPosting:" + name]))
                throw new InvalidOperationException("Missing SYSPRO posting configuration: " + name);
        if (!Uri.TryCreate(config["SysproPosting:ServiceUrl"], UriKind.Absolute, out var endpoint) ||
            (endpoint.Scheme != "http" && endpoint.Scheme != "https"))
            throw new InvalidOperationException("SYSPRO ServiceUrl must be an HTTP or HTTPS base URL.");
        var dir = config["SysproPosting:ContractsDirectory"]!;
        var missing = new[] { "SORTOS.success.xpath" }
            .Where(name => !File.Exists(Path.Combine(dir, name))).ToList();
        if (missing.Count != 0)
            throw new InvalidOperationException("Missing SYSPRO mapping files: " + string.Join(", ", missing) + ". No order has been changed.");
        foreach (var step in new[] { "SORTOS", "SORTOI", "SORTSU", "SORTOX" })
        {
            var builtIn = step == "SORTOS" || step == "SORTSU" || step == "SORTOI" || step == "SORTOX";
            var requestPath = Path.Combine(dir, step + ".request.xslt");
            if (!builtIn || File.Exists(requestPath)) Load(step + ".request", requestPath);
            {
                var parameterPath = Path.Combine(dir, step + ".parameters.xml");
                var xml = builtIn && !File.Exists(parameterPath) ? (step == "SORTOI" ? SortoiMapping.Parameters() : PostingXml.Parameters(step)).ToString() : File.ReadAllText(parameterPath);
                ValidateParameters(step, Parse(xml));
                parameters[step] = xml;
            }
            // SORTSU acknowledgement is bypassed by request; final entity readback verifies the outcome.
            if (step == "SORTSU" || step == "SORTOX") continue;
            var successPath = Path.Combine(dir, step + ".success.xpath");
            if (step == "SORTOI" && !File.Exists(successPath)) continue;
            var expression = File.ReadAllText(successPath).Trim();
            XPathExpression.Compile(expression);
            success[step] = expression;
        }
        var importResultPath = Path.Combine(dir, "SORTOI.result.xslt");
        if (File.Exists(importResultPath)) Load("SORTOI.result", importResultPath);

    }
    public static void ValidateParameters(string step, XElement xml)
    {
        string? Value(string name) => xml.Descendants(name).SingleOrDefault()?.Value.Trim();
        if (step == "SORTOI" && !string.Equals(Value("Process"), "Import", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("SORTOI must use Process Import, not a validation-only operation.");
        if ((step == "SORTOS" || step == "SORTSU" || step == "SORTOX") &&
            (Value("ValidateOnly") != "N" || Value("ApplyIfEntireDocumentValid") != "Y"))
            throw new InvalidOperationException(step + " must post atomically with ValidateOnly N and ApplyIfEntireDocumentValid Y.");
    }
    private void Load(string key, string path)
    {
        var transform = new XslCompiledTransform();
        using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
        transform.Load(reader, XsltSettings.Default, null);
        transforms[key] = transform;
    }
    public string Request(string step, XElement input)
    {
        var output = transforms.ContainsKey(step + ".request")
            ? Transform(step + ".request", input)
            : step switch {
                "SORTOI" => SortoiMapping.Request(input),
                "SORTOS" => PostingXml.ReleaseOriginalForInvoicing(input),
                "SORTSU" => PostingXml.ChangeComponents(input),
                "SORTOX" => PostingXml.CompleteZeroComponents(input),
                _ => throw new InvalidOperationException("No request mapping for " + step)
            };
        if (step == "SORTOI")
        {
            var expected = input.Element("Lines")?.Elements("Line")
                .Where(x => ((string?)x.Element("KitFlag"))?.Trim() == "P" && string.IsNullOrWhiteSpace((string?)x.Element("ParentLine"))).ToList() ?? [];
            var actual = output.Descendants("StockLine").ToList();
            if (expected.Count == 0 || actual.Count != expected.Count || expected.Any(line =>
                actual.Count(stock =>
                    (string?)stock.Element("CustomerPoLine") == (string?)line.Element("LineNumber") &&
                    (string?)stock.Element("StockCode") == (string?)line.Element("StockCode")) != 1))
                throw new InvalidOperationException("SORTOI must include only kit parent lines, exactly once each. Update or remove the SORTOI.request.xslt override in ContractsDirectory.");
        }
        if (step == "SORTOX" || step == "SORTSU")
        {
            var expected = input.Element("Lines")!.Elements("Line").Where(x => PostingXml.IsComponent(x) &&
                (step == "SORTOX" ? PostingXml.Quantity(x) == 0 : PostingXml.Quantity(x) > 0)).ToList();
            var actual = output.Elements("Item").ToList();
            if (output.Name != (step == "SORTOX" ? "PostSalesOrderCancel" : "PostChangeSalesOrderKitComp") ||
                actual.Count != expected.Count || expected.Any(line => actual.Count(item =>
                    item.Elements("SalesOrder").Count() == 1 && item.Elements("SalesOrderLine").Count() == 1 &&
                    (string?)item.Element("SalesOrder") == (string?)input.Element("SalesOrder") &&
                    int.TryParse((string?)item.Element("SalesOrderLine"), out var number) && number > 0 &&
                    number == (int?)line.Element("LineNumber") &&
                    (step == "SORTOX" ? item.Element("MarkLineComplete")?.Value == "Y" &&
                        string.IsNullOrWhiteSpace(item.Element("CancelReasonCode")?.Value) :
                        item.Element("QuantityType")?.Value == "T" &&
                        (string?)item.Element("StockCode") == (string?)line.Element("StockCode") &&
                        decimal.TryParse((string?)item.Element("OrderQty"), System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.InvariantCulture, out var qty) && qty == PostingXml.Quantity(line))) != 1))
                throw new InvalidOperationException(step + " mapping does not match the checked component plan.");
        }
        return output.ToString(SaveOptions.DisableFormatting);
    }
    public string Parameters(string step) => parameters.GetValueOrDefault(step, step == "SORTOI" ? SortoiMapping.Parameters().ToString() : "");
    public XElement Result(string step, XElement input) => step == "SORTOI" && !transforms.ContainsKey(step + ".result")
        ? SortoiMapping.Result(input) : Transform(step + ".result", input);
    private XElement Transform(string key, XElement input)
    {
        using var reader = input.CreateReader();
        using var output = new StringWriter();
        transforms[key].Transform(reader, null, output);
        return Parse(output.ToString());
    }
    public void CheckSuccess(string step, XElement output)
    {
        CheckErrors(output);
        if (step == "SORTSU")
        {
            if (!output.Name.LocalName.Equals("postchangesalesorderkitcomp", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SORTSU returned an unexpected response document.");
            var hasErrorCode = output.Descendants().Any(x => x.Name.LocalName == "ErrorNumber" &&
                !string.IsNullOrWhiteSpace(x.Value) && (!long.TryParse(x.Value.Trim(), out var code) || code != 0));
            var failed = output.Descendants().Where(x => x.Name.LocalName == "ValidationStatus")
                .SelectMany(x => x.Elements().Where(e => e.Name.LocalName == "Status"))
                .Any(x => new[] { "Failed", "Error", "Rejected" }.Contains(x.Value.Trim(), StringComparer.OrdinalIgnoreCase));
            if (hasErrorCode || failed) throw new InvalidOperationException("SORTSU returned an explicit failure.");
            // No positive success XPath is required. Completion still requires final entity verification.
            return;
        }
        if (step == "SORTOI" && !success.ContainsKey(step)) { SortoiMapping.Result(output); return; }
        var result = new XDocument(new XElement(output)).XPathEvaluate(success[step]);
        if (result is not bool ok || !ok)
            throw new InvalidOperationException(step + " did not return the configured positive success acknowledgement.");
    }
    public static XElement Parse(string xml)
    {
        using var input = new StringReader(xml);
        using var reader = XmlReader.Create(input, new XmlReaderSettings {
            DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 10 * 1024 * 1024
        });
        return XElement.Load(reader);
    }
    public static void CheckErrors(XElement xml)
    {
        var nodes = xml.DescendantsAndSelf().ToList();
        var descriptions = nodes.Where(x => x.Name.LocalName.Equals("ErrorDescription", StringComparison.OrdinalIgnoreCase) ||
            x.Name.LocalName.Equals("ErrorMessage", StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Value.Trim()).Where(x => x.Length > 0).ToList();
        var invalid = nodes.Any(x => new[] { "ItemsInvalid", "OrdersInvalid", "LinesInvalid", "ErrorCount", "ItemsRejectedWithWarnings" }
            .Contains(x.Name.LocalName, StringComparer.OrdinalIgnoreCase) &&
            (!int.TryParse(x.Value.Trim(), out var count) || count != 0));
        if (descriptions.Count > 0 || invalid)
            throw new InvalidOperationException(descriptions.Count > 0 ? string.Join("; ", descriptions) : "SYSPRO returned invalid items.");
    }
}
