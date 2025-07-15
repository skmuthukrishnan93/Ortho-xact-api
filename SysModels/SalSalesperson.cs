using System;
using System.Collections.Generic;

namespace Ortho_xact_api.SysModels;

public partial class SalSalesperson
{
    public string Branch { get; set; } = null!;

    public string Salesperson { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal SalesBudget1 { get; set; }

    public decimal SalesBudget2 { get; set; }

    public decimal SalesBudget3 { get; set; }

    public decimal SalesBudget4 { get; set; }

    public decimal SalesBudget5 { get; set; }

    public decimal SalesBudget6 { get; set; }

    public decimal SalesBudget7 { get; set; }

    public decimal SalesBudget8 { get; set; }

    public decimal SalesBudget9 { get; set; }

    public decimal SalesBudget10 { get; set; }

    public decimal SalesBudget11 { get; set; }

    public decimal SalesBudget12 { get; set; }

    public decimal SalesBudget13 { get; set; }

    public decimal SalesActual1 { get; set; }

    public decimal SalesActual2 { get; set; }

    public decimal SalesActual3 { get; set; }

    public decimal SalesActual4 { get; set; }

    public decimal SalesActual5 { get; set; }

    public decimal SalesActual6 { get; set; }

    public decimal SalesActual7 { get; set; }

    public decimal SalesActual8 { get; set; }

    public decimal SalesActual9 { get; set; }

    public decimal SalesActual10 { get; set; }

    public decimal SalesActual11 { get; set; }

    public decimal SalesActual12 { get; set; }

    public decimal SalesActual13 { get; set; }

    public decimal CommissionPct { get; set; }

    public string SalespersonColl { get; set; } = null!;

    public byte[]? TimeStamp { get; set; }

    public virtual ICollection<SorMaster> SorMasters { get; set; } = new List<SorMaster>();
}
