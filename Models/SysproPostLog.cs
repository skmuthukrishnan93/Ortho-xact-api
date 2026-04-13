using System;
using System.Collections.Generic;

namespace Ortho_xact_api.Models;

public partial class SysproPostLog
{
    public long Id { get; set; }

    public string? Event { get; set; }

    public string? EventMessage { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? CreatedTime { get; set; }

    public string? Action { get; set; }

    public string? SalesOrder { get; set; }
}
