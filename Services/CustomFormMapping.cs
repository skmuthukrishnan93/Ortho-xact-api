using System.Globalization;
using System.Xml.Linq;
using Ortho_xact_api.Models;

namespace Ortho_xact_api.Services;

public record PatientDetailsValues(string PatientNumber, string PatientName, string DoctorsName,
    DateTime ProcedureDate, decimal CgSetNumber);

// COMTFM mapping uses the ORD custom-form document supplied by the client.
public static class CustomFormMapping
{
    public static XElement Request(XElement plan, IConfiguration config)
    {
        string Value(XElement? node, string name) => node?.Element(name)?.Value.Trim() ?? "";
        var header = plan.Element("Header")!;
        var values = Validate(new SorHeaderStage {
            PatientNumber = Value(header, "PatientNumber"), PatientName = Value(header, "PatientName"),
            SurgeonName = Value(header, "SurgeonName"), CgSetNumber = Value(header, "CgSetNumber"),
            ProcedureDate = DateOnly.TryParseExact(Value(header, "ProcedureDate"), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) ? date : default
        });
        var order = Value(plan, "SalesOrder");
        var fields = new XElement("Fields",
            new XElement("PatientNumber", values.PatientNumber), new XElement("PatientName", values.PatientName),
            new XElement("DoctorsName", values.DoctorsName),
            new XElement("ProcedureDate", values.ProcedureDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement("CgSetNumber", values.CgSetNumber.ToString(CultureInfo.InvariantCulture)));
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
        return new XElement("PostCustomForm", new XAttribute(XNamespace.Xmlns + "xsd", xsi),
            new XAttribute(xsi + "noNamespaceSchemaLocation", "COMTFMDOC.XSD"),
            new XElement("Item", new XElement("Key", new XElement("FormType", "ORD"),
                new XElement("KeyFields", new XElement("SalesOrder", order.Length == 0 ? "" : OrderKey(order)),
                    new XElement("InvoiceNumber"))), fields));
    }

    public static XElement Parameters() => new("PostCustomForm", new XElement("Parameters",
        new XElement("Function", "U"), new XElement("ValidateOnly", "N"),
        new XElement("ApplyIfEntireDocumentValid", "N")));

    public static void CheckSuccess(XElement response)
    {
        ValidateResponse(response);
        var status = response.Elements().SingleOrDefault(x => x.Name.LocalName == "StatusOfItems");
        var read = status?.Elements().SingleOrDefault(x => x.Name.LocalName == "RecordsRead")?.Value;
        var invalid = status?.Elements().SingleOrDefault(x => x.Name.LocalName == "RecordsInvalid")?.Value;
        var written = status?.Elements().SingleOrDefault(x => x.Name.LocalName == "RecordsWritten")?.Value;
        if (!int.TryParse(read, out var count) || count != 1 ||
            !int.TryParse(invalid, out var errors) || errors != 0 ||
            !int.TryParse(written, out var committed) || committed != 1 ||
            response.Elements().Count(x => x.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase)) != 1)
            throw new InvalidOperationException("COMTFM must confirm RecordsRead 1, RecordsInvalid 0 and RecordsWritten 1 for one custom-form item.");
    }

    public static void ValidateResponse(XElement response)
    {
        PostingContracts.CheckErrors(response);
        if (!response.Name.LocalName.Equals("PostCustomForm", StringComparison.OrdinalIgnoreCase) ||
            response.Descendants().Any(x => x.Name.LocalName == "ErrorNumber" &&
                !string.IsNullOrWhiteSpace(x.Value) && (!long.TryParse(x.Value, out var code) || code != 0)) ||
            response.Descendants().Any(x => x.Name.LocalName == "Status" &&
                new[] { "Failed", "Error", "Rejected" }.Contains(x.Value.Trim(), StringComparer.OrdinalIgnoreCase)) ||
            response.Descendants().Any(x => x.Name.LocalName == "ValidateOnly" && x.Value.Trim() == "Y"))
            throw new InvalidOperationException("COMTFM returned an unexpected document or an explicit failure.");
    }

    public static void VerifyReturnedKey(XElement request, XElement response)
    {
        var expected = request.Element("Item")!.Element("Key")!;
        var keys = response.Elements().Where(x => x.Name.LocalName.Equals("Item", StringComparison.OrdinalIgnoreCase))
            .SelectMany(x => x.Elements().Where(k => k.Name.LocalName.Equals("Key", StringComparison.OrdinalIgnoreCase))).ToList();
        if (keys.Count != 1)
            throw new InvalidOperationException("COMTFM must return exactly one custom-form key.");
        foreach (var key in keys)
        {
            string? Value(XElement node, string name) => node.Descendants().SingleOrDefault(x =>
                x.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value.Trim();
            var order = Value(key, "SalesOrder");
            if (Value(key, "FormType") != "ORD" || string.IsNullOrWhiteSpace(order) ||
                OrderKey(order) != Value(expected, "SalesOrder") || Value(key, "InvoiceNumber") != "" ||
                (Value(key, "keyvalue") is string keyValue && OrderKey(keyValue) != Value(expected, "SalesOrder")))
                throw new InvalidOperationException("COMTFM response refers to a different custom-form key.");
        }
    }
    public static PatientDetailsValues Validate(SorHeaderStage procedure)
    {
        static string Required(string value, string name)
        {
            value = value.Trim();
            if (value.Length is < 1 or > 200)
                throw new InvalidOperationException(name + " must contain 1 to 200 characters for CusSorMaster+.");
            return value;
        }
        var number = Required(procedure.PatientNumber, "Patient Number");
        var name = Required(procedure.PatientName, "Patient Name");
        var doctor = Required(procedure.SurgeonName, "Surgeon Name");
        if (procedure.ProcedureDate < new DateOnly(1753, 1, 1))
            throw new InvalidOperationException("Procedure Date is outside the SYSPRO datetime range.");
        if (!decimal.TryParse(procedure.CgSetNumber.Trim(), NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var set) || set < -999999999999m || set > 999999999999m)
            throw new InvalidOperationException("CG Set Number must be a whole numeric value of at most 12 digits for CusSorMaster+. Text such as TestSet is not supported by its decimal(12,0) column.");
        return new(number, name, doctor, procedure.ProcedureDate.ToDateTime(TimeOnly.MinValue), set);
    }

    public static string OrderKey(string order)
    {
        var key = ProcedureStagingService.Normalize(order);
        if (key.Length is < 1 or > 20)
            throw new InvalidOperationException("A valid replacement sales order is required for patient details.");
        return key;
    }
}
