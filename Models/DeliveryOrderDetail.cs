using System;
using System.Collections.Generic;

namespace Ortho_xact_api.Models;

public partial class DeliveryOrderDetail
{
    public long Id { get; set; }

    public string SalesOrder { get; set; } = null!;

    public int Line { get; set; }

    public string? Status { get; set; }

    public int? Sysprostatus { get; set; }

    public string? Set { get; set; }

    public string? Mwarehouse { get; set; }

    public string? MstockCode { get; set; }

    public string? MstockDes { get; set; }

    public decimal? MorderQty { get; set; }

    public decimal? MshipQty { get; set; }

    public decimal? RepUsageQty { get; set; }

    public decimal? RetQty { get; set; }

    public decimal? Usage { get; set; }

    public decimal? Variance { get; set; }

    public string? RepName { get; set; }

    public DateTime? RepEntertedDate { get; set; }

    public string? ClerkName { get; set; }

    public DateTime? ClerkDate { get; set; }

    public string? Customer { get; set; }

    public string? CustomerName { get; set; }

    public string? RepVerNumber { get; set; }

    public string? ClerkVerNumber { get; set; }

    public string? AdminVerNumber { get; set; }

    public string? PostedBy { get; set; }

    public DateTime? PostedDate { get; set; }
}
