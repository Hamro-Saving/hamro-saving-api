using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Infrastructure.Email;

namespace IntegrationTests.Notifications;

/// <summary>
/// The person an event happened to should not read about themselves in the third person.
/// Only the subject and opening sentence vary; it is still the same event.
/// </summary>
public class EmailLayoutTests
{
    private const string GroupSubject = "Sita Rai has made a monthly deposit for Bhadra 2082";
    private const string GroupHeadline = "Sita Rai has made a monthly deposit of Rs. 5,000.00 for Bhadra 2082.";

    private static readonly SelfAddressed AsSita = new(
        "sita@example.com",
        "Your monthly deposit for Bhadra 2082 has been verified",
        "Your monthly deposit of Rs. 5,000.00 for Bhadra 2082 has been verified and is on the group's books.");

    private static (string Subject, string Headline) For(string email, SelfAddressed? self) =>
        EmailLayout.For(new EmailRecipient(email, "Reader"), GroupSubject, GroupHeadline, self);

    [Fact]
    public void TheSubjectIsAddressedDirectly()
    {
        var (subject, headline) = For("sita@example.com", AsSita);

        Assert.Equal(AsSita.Subject, subject);
        Assert.StartsWith("Your monthly deposit", headline);
    }

    [Fact]
    public void EveryoneElseGetsTheGroupsAccountOfIt()
    {
        var (subject, headline) = For("ram@example.com", AsSita);

        Assert.Equal(GroupSubject, subject);
        Assert.Equal(GroupHeadline, headline);
    }

    [Fact]
    public void TheMatchDoesNotTurnOnHowTheAddressWasCased()
    {
        // Domain rows are lower-cased on the way in, but nothing guarantees a recipient list
        // assembled elsewhere is, and matching the wrong way round means the subject silently
        // gets the third-person version — a failure that looks like nothing went wrong.
        var (_, headline) = For("Sita@Example.com", AsSita);

        Assert.StartsWith("Your monthly deposit", headline);
    }

    [Fact]
    public void AnEmailWithNoSubjectReadsTheSameToEverybody()
    {
        var (subject, headline) = For("sita@example.com", self: null);

        Assert.Equal(GroupSubject, subject);
        Assert.Equal(GroupHeadline, headline);
    }

    [Fact]
    public void ASubjectWithNoEmailIsNotAddressedAtAll()
    {
        // A borrower recorded without an address is a real case: the group keeps the loan,
        // they simply never hear about it.
        Assert.Null(SelfAddressed.To(null, "s", "h"));
        Assert.Null(SelfAddressed.To("", "s", "h"));
        Assert.NotNull(SelfAddressed.To("hari@example.com", "s", "h"));
    }
}
