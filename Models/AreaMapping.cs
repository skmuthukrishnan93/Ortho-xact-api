using System;
using System.Collections.Generic;

namespace Ortho_xact_api.Models;

public partial class AreaMapping
{
    public long Id { get; set; }

    public string? Area { get; set; }

    public string? Username { get; set; }

    public string? MappedBy { get; set; }

    public DateTime? MappedDate { get; set; }
}
