using System.ComponentModel.DataAnnotations;

namespace Ortho_xact_api.DTO;

public class PatientProcedureDto
{
    [Range(1, int.MaxValue)] public int ProcedureNumber { get; set; }
    [Required, MaxLength(200)] public string PatientNumber { get; set; } = "";
    [Required, MaxLength(200)] public string PatientName { get; set; } = "";
    [Required, MaxLength(200)] public string SurgeonName { get; set; } = "";
    public DateOnly ProcedureDate { get; set; }
    [Required, MaxLength(200)] public string CgSetNumber { get; set; } = "";
    [Required, MinLength(1)] public List<int> LineNumbers { get; set; } = new();
}
