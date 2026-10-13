namespace Ortho_xact_api.Models;

// Application staging tables; never mapped to the SYSPRO database.
public class SorHeaderStage
{
    public string SalesOrder { get; set; } = "";
    public int ProcedureNumber { get; set; }
    public string PatientNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string SurgeonName { get; set; } = "";
    public DateOnly ProcedureDate { get; set; }
    public string CgSetNumber { get; set; } = "";
    public string Customer { get; set; } = "";
    public string SourceHeaderJson { get; set; } = "{}";
}

public class SorDetailStage
{
    public string SalesOrder { get; set; } = "";
    public int Line { get; set; }
    public int ProcedureNumber { get; set; }
    public int? ParentLine { get; set; }
    public string KitFlag { get; set; } = "";
    public string ComponentSequence { get; set; } = "";
    public string SourceLineJson { get; set; } = "{}";
    public string SetCode { get; set; } = "";
    public string StockCode { get; set; } = "";
    public string Warehouse { get; set; } = "";
    public string Bin { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal DeliveredQty { get; set; }
    public decimal OrderQty { get; set; }
    public decimal RepUsageQty { get; set; }
    public decimal? ReturnQty { get; set; }
    public decimal? UsageQty { get; set; }
    public decimal? Variance { get; set; }
    public DateTime? CheckedAt { get; set; }
    public string Status { get; set; } = "RepCompleted";
}

public class ProcedurePostAttempt
{
    public Guid Id { get; set; }
    public string SalesOrder { get; set; } = "";
    public string Status { get; set; } = "Posting";
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public string SnapshotJson { get; set; } = "";
}

public class ProcedurePostStep
{
    public long Id { get; set; }
    public Guid AttemptId { get; set; }
    public int? ProcedureNumber { get; set; }
    public string BusinessObject { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public string RequestXml { get; set; } = "";
    public string ParametersXml { get; set; } = "";
    public string ResponseXml { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class UnsuccessfulSOMaster
{
    public Guid Id { get; set; }
    public string SalesOrder { get; set; } = "";
    public string Error { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? NotificationSentAt { get; set; }
    public string? NotificationError { get; set; }
}

public class UnsuccessfulSODetail
{
    public long Id { get; set; }
    public Guid MasterId { get; set; }
    public int? ProcedureNumber { get; set; }
    public string BusinessObject { get; set; } = "";
    public string RequestXml { get; set; } = "";
    public string ResponseXml { get; set; } = "";
}
