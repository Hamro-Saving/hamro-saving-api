namespace HamroSavings.Infrastructure.Email;

/// <summary>
/// Who a message appears to come from, so a person in several groups can tell them apart.
///
/// The domain is deliberately not derived from the group: mail from a domain the sending
/// server is not authorised for fails SPF and DMARC, so the configured domain stays and the
/// group goes in the local part, which providers do let a sender vary.
/// </summary>
internal static class SenderIdentity
{
    /// <summary>Local parts are capped at 64 characters; this leaves room for the prefix.</summary>
    private const int MaxSlugLength = 40;

    /// <summary>
    /// No fallback needed: a group's name is required by its validators and its column.
    ///
    /// It is admin-typed text shown as the sender. MimeKit quotes it, so it cannot forge a
    /// header — but a group named after a bank will read as one, which matters if these ever
    /// reach beyond the group's own members.
    /// </summary>
    public static string DisplayName(string groupName) => groupName;

    /// <summary>
    /// <c>noreply@example.com</c> becomes <c>noreply+sunrise-savers@example.com</c>. A name
    /// that slugs to nothing — Devanagari, say — keeps the plain address rather than
    /// producing an invalid one.
    /// </summary>
    public static string Address(string groupName, string baseAddress)
    {
        var at = baseAddress.IndexOf('@');
        if (at <= 0 || at == baseAddress.Length - 1) return baseAddress;

        var slug = Slug(groupName);
        if (slug.Length == 0) return baseAddress;

        return $"{baseAddress[..at]}+{slug}@{baseAddress[(at + 1)..]}";
    }

    private static string Slug(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName)) return string.Empty;

        var slug = new string([.. groupName.ToLowerInvariant()
            .Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')]);

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        slug = slug.Trim('-');

        return slug.Length > MaxSlugLength ? slug[..MaxSlugLength].TrimEnd('-') : slug;
    }
}
