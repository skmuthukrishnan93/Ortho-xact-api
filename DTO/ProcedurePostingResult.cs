namespace Ortho_xact_api.DTO;

public record CreatedSalesOrder(int ProcedureNumber, string SalesOrder);
public record ProcedurePostingResult(Guid AttemptId, string OriginalSalesOrder,
    IReadOnlyList<CreatedSalesOrder> CreatedSalesOrders);
