using System;
using System.Collections.Generic;

namespace Ortho_xact_api.SysModels;

public partial class SalArea
{
    public string Area { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string TaxCode { get; set; } = null!;

    public string ProvTaxFlag { get; set; } = null!;

    public string AreaGstCode { get; set; } = null!;

    public byte[]? TimeStamp { get; set; }

    public virtual ICollection<SorMaster> SorMasters { get; set; } = new List<SorMaster>();
}
