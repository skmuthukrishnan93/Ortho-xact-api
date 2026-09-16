namespace Ortho_xact_api.Models;

public class PatientProcedure
{
    public string SalesOrder { get; set; } = "";
    public int ProcedureNumber { get; set; }
    public string PatientNumber { get; set; } = "";
    public string PatientName { get; set; } = "";
    public string SurgeonName { get; set; } = "";
    public DateOnly ProcedureDate { get; set; }
    public string CgSetNumber { get; set; } = "";
    public string LineNumbersJson { get; set; } = "[]";
}
