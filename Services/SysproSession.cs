namespace Ortho_xact_api.Services;

/// <summary>Validates the UserID returned by SYSPRO utilities Logon.</summary>
public static class SysproSession
{
    public static string Parse(string? response)
    {
        var session = response?.Trim() ?? "";
        // SYSPRO 8 utilities also returns SYS800-<32 hex GUID>-<6 digits>-<3 digits>.
        // This is an opaque session token: never strip the prefix or routing suffixes.
        if (System.Text.RegularExpressions.Regex.IsMatch(session,
                @"\ASYS800-[0-9A-Fa-f]{32}-[0-9]{6}-[0-9]{3}\z")
            && Guid.TryParseExact(session.Substring(7, 32), "N", out var syspro8Id)
            && syspro8Id != Guid.Empty)
            return session;

        // UserID: 32 hex GUID characters + instance digit + user type (0, 1 or 2).
        // Older e.net versions return 33 characters (GUID + instance).
        // Preserve the entire token, including its suffix and casing.
        if (session.Length is 33 or 34
            && Guid.TryParseExact(session[..32], "N", out var id) && id != Guid.Empty
            && session[32] is >= '0' and <= '9'
            && (session.Length == 33 || session[33] is >= '0' and <= '2'))
            return session;

        // Retain compatibility with services returning a conventional GUID.
        if (Guid.TryParse(session, out var guid) && guid != Guid.Empty)
            return session;

        // Do not expose the response: it may contain credentials or a session token.
        throw new InvalidOperationException(session.Length == 0
            ? "SYSPRO login returned an empty session. Check the configured utilities service, operator and company."
            : "SYSPRO login returned an invalid session format. Check SYSPRO login diagnostics for the configured operator and company.");
    }
}
