using System;
using System.Collections.Generic;

namespace Ortho_xact_api.SysModels;

public partial class VwFetchSordetail
{
    public string MbomFlag { get; set; } = null!;

    public string OrderStatus { get; set; } = null!;

    public decimal? RetQty { get; set; }

    public decimal? Usage { get; set; }

    public decimal? Variance { get; set; }

    public string? Status { get; set; }

    public decimal? RepUsageQty { get; set; }

    public string SalesOrder { get; set; } = null!;

    public decimal SalesOrderLine { get; set; }

    public string MstockCode { get; set; } = null!;

    public string MstockDes { get; set; } = null!;

    public string Mwarehouse { get; set; } = null!;

    public decimal MorderQty { get; set; }

    public decimal MshipQty { get; set; }

    public string SetsCode { get; set; } = null!;

    public string? Setsdec { get; set; }

    public string Customer { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string Salesperson { get; set; } = null!;

    public string Area { get; set; } = null!;

    public DateTime? RepEntertedDate { get; set; }

    public DateTime? ClerkDate { get; set; }

    public DateTime? OrderDate { get; set; }
}
