using HamroSavings.Application.Abstractions.Email;
using System.Net;
using System.Text;

namespace HamroSavings.Infrastructure.Email;

internal sealed record EmailDetail(string Label, string Value);

/// <summary>
/// The email in the second person for the one person it is about — a third-person account of
/// your own deposit reads like it was meant for someone else.
/// </summary>
internal sealed record SelfAddressed(string Email, string Subject, string Headline)
{
    /// <summary>Nothing when the subject has no email — a borrower recorded without one is a real case.</summary>
    public static SelfAddressed? To(string? email, string subject, string headline) =>
        string.IsNullOrEmpty(email) ? null : new SelfAddressed(email, subject, headline);
}

/// <summary>
/// Shared chrome, so a dozen emails look like one system. What each says is decided by
/// <see cref="EmailService"/>; this only decides how it looks.
///
/// Every email signs off with the group's name, not the product's: a person in several groups
/// needs to know whose books they are reading about.
/// </summary>
internal static class EmailLayout
{
    /// <summary>Only these two vary by reader; the figures and the link are the same event for everybody.</summary>
    public static (string Subject, string Headline) For(
        EmailRecipient recipient, string subject, string headline, SelfAddressed? self) =>
        self is not null && string.Equals(self.Email, recipient.Email, StringComparison.OrdinalIgnoreCase)
            ? (self.Subject, self.Headline)
            : (subject, headline);

    public static string Html(
        EmailRecipient recipient,
        string groupName,
        string headline,
        IReadOnlyList<EmailDetail> details,
        string? footnote,
        string? actionLabel,
        string? link)
    {
        var rows = new StringBuilder();
        foreach (var detail in details)
        {
            rows.Append($"""
                <tr>
                    <td style="padding:6px 16px 6px 0;color:#6b7280;white-space:nowrap;">{Escape(detail.Label)}</td>
                    <td style="padding:6px 0;color:#111827;font-weight:600;">{Escape(detail.Value)}</td>
                </tr>
                """);
        }

        var button = link is null || actionLabel is null
            ? string.Empty
            : $"""
                <p style="margin:24px 0 0;">
                    <a href="{Escape(link)}" style="display:inline-block;background-color:#4f46e5;color:#ffffff;padding:12px 24px;text-decoration:none;border-radius:6px;font-weight:bold;">{Escape(actionLabel)}</a>
                </p>
                """;

        var closing = footnote is null
            ? string.Empty
            : $"""<p style="margin:16px 0 0;color:#6b7280;">{Escape(footnote)}</p>""";

        return $"""
            <div style="font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:15px;line-height:1.6;color:#111827;">
                <p>Hello {Escape(recipient.Name)},</p>
                <p style="font-size:17px;">{Escape(headline)}</p>
                {Table(details, rows)}
                {closing}
                {button}
                <p style="margin-top:32px;color:#6b7280;font-size:13px;">— {Escape(groupName)}</p>
            </div>
            """;
    }

    public static string Text(
        EmailRecipient recipient,
        string groupName,
        string headline,
        IReadOnlyList<EmailDetail> details,
        string? footnote,
        string? actionLabel,
        string? link)
    {
        var body = new StringBuilder()
            .Append("Hello ").Append(recipient.Name).AppendLine(",")
            .AppendLine()
            .AppendLine(headline)
            .AppendLine();

        foreach (var detail in details)
        {
            body.Append("  ").Append(detail.Label).Append(": ").AppendLine(detail.Value);
        }

        if (footnote is not null)
        {
            body.AppendLine().AppendLine(footnote);
        }

        if (link is not null)
        {
            body.AppendLine().Append(actionLabel ?? "View").Append(": ").AppendLine(link);
        }

        return body.AppendLine().Append("— ").Append(groupName).ToString();
    }

    /// <summary>An invite has no figures, and an empty table still renders as a gap.</summary>
    private static string Table(IReadOnlyList<EmailDetail> details, StringBuilder rows) =>
        details.Count == 0
            ? string.Empty
            : $"""<table style="border-collapse:collapse;margin:16px 0;">{rows}</table>""";

    /// <summary>Names and notes are text people typed, not markup, and should arrive looking that way.</summary>
    private static string Escape(string value) => WebUtility.HtmlEncode(value);
}
