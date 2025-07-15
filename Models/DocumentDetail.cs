using System;
using System.Collections.Generic;

namespace Ortho_xact_api.Models;

public partial class DocumentDetail
{
    public long DocId { get; set; }

    public byte[]? Document { get; set; }

    public DateTime? CreatedTime { get; set; }

    public int? DocType { get; set; }

    public string? DocNumber { get; set; }
}
