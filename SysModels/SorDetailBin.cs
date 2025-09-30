using System;
using System.Collections.Generic;

namespace Ortho_xact_api.SysModels;

public partial class SorDetailBin
{
    public string SalesOrder { get; set; } = null!;

    public decimal SalesOrderLine { get; set; }

    public string Lot { get; set; } = null!;

    public string Bin { get; set; } = null!;

    public decimal StockQtyToShip { get; set; }

    public decimal QtyThisSession { get; set; }

    public decimal QtyReserved { get; set; }

    public byte[]? TimeStamp { get; set; }

    public virtual SorMaster SalesOrderNavigation { get; set; } = null!;

    public virtual SorDetail SorDetail { get; set; } = null!;
}
