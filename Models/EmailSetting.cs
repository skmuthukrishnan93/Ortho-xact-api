using System;
using System.Collections.Generic;

namespace Ortho_xact_api.Models;

public partial class EmailSetting
{
    public long Id { get; set; }

    public string? Subject { get; set; }

    public string? Body { get; set; }

    public string? Signature { get; set; }

    public string? Cc { get; set; }

    public string? Bcc { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public string? SmtpServer { get; set; }

    public string? PortNumber { get; set; }

    public string? FromAddress { get; set; }

    public string? Password { get; set; }
}
