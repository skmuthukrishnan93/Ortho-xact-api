using System;
using System.Collections.Generic;

namespace Ortho_xact_api.Models;

public partial class User
{
    public int Id { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string? Firstname { get; set; }

    public string? Lastname { get; set; }

    public string? Email { get; set; }

    public string? Roles { get; set; }

    public bool? Active { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public string? Salesperson { get; set; }

    public string? CustomerEmail { get; set; }
}
