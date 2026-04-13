namespace Ortho_xact_api.DTO
{
    public class UpdateOrderStatusRequest
    {
        public string? SalesOrderNumber { get; set; }
        public string? CorrectionType { get; set; }  // "repclerk" or "rep"
        public string? RouteToRepClerk { get; set; }
        public string? RouteToRep { get; set; }
        public string? Reason { get; set; }
    }
}
