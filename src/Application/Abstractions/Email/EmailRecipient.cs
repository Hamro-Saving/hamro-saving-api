namespace HamroSavings.Application.Abstractions.Email;

/// <summary>Who an email goes to. Name is what the greeting uses.</summary>
public sealed record EmailRecipient(string Email, string Name);
