namespace Ortho_xact_api.Services;

public sealed class ProcedurePostingException(string message, Guid attemptId, string detail,
    string? businessObject, int? procedureNumber, string responseXml, Exception inner)
    : InvalidOperationException(message, inner)
{
    public Guid AttemptId { get; } = attemptId;
    public string Detail { get; } = detail;
    public string? BusinessObject { get; } = businessObject;
    public int? ProcedureNumber { get; } = procedureNumber;
    public string ResponseXml { get; } = responseXml;
}
