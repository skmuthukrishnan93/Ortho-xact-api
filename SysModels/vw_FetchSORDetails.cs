using System;
using System.Collections.Generic;

namespace Ortho_xact_api.SysModels;

public partial class vw_FetchSORDetails
{
    public string MBomFlag { get; set; } = null!;

    public string OrderStatus { get; set; } = null!;

    public decimal? RetQty { get; set; }

    public decimal? Usage { get; set; }

    public decimal? variance { get; set; }

    public string? Status { get; set; }

    public decimal? RepUsageQty { get; set; }

    public string SalesOrder { get; set; } = null!;

    public decimal SalesOrderLine { get; set; }

    public string MStockCode { get; set; } = null!;

    public string MStockDes { get; set; } = null!;

    public string MWarehouse { get; set; } = null!;

    public decimal MOrderQty { get; set; }

    public decimal MShipQty { get; set; }

    public string SetsCode { get; set; } = null!;

    public string? Setsdec { get; set; }

    public string Customer { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string Salesperson { get; set; } = null!;

    public string Area { get; set; } = null!;
}
