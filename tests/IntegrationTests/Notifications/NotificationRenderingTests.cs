using HamroSavings.Application;
using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Groups;
using HamroSavings.Domain.Ledger;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Savings;
using HamroSavings.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;

namespace IntegrationTests.Notifications;

/// <summary>
/// Driven through the same methods the handlers call. Composition and rendering are exercised
/// together because that is how they run — nothing in between is reachable from outside.
/// </summary>
public class NotificationRenderingTests
{
    private const string FrontendUrl = "https://app.example.com";
    /// <summary>The group every email in these tests speaks for, and signs off as.</summary>
    private static readonly Group TheGroup = Group.Create("Sunrise Savers", "SUN");
    private static readonly DateTime Today = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed class CapturingSender : IEmailSender
    {
        public List<(string Recipient, string FromName, string Subject, string? Html, string? Text)> Sent { get; } = [];

        public Task SendAsync(string recipient, string fromName, string subject, string? htmlBody, string? textBody, CancellationToken ct)
        {
            Sent.Add((recipient, fromName, subject, htmlBody, textBody));
            return Task.CompletedTask;
        }
    }

    private sealed class FlakySender(string failsFor) : IEmailSender
    {
        public List<string> Delivered { get; } = [];

        public Task SendAsync(string recipient, string fromName, string subject, string? htmlBody, string? textBody, CancellationToken ct)
        {
            if (recipient == failsFor) throw new InvalidOperationException("mailbox unavailable");
            Delivered.Add(recipient);
            return Task.CompletedTask;
        }
    }

    private static (IEmailService Service, CapturingSender Sender, IDisposable Scope) Harness(IEmailSender? substitute = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HamroSavingsDb"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Jwt:Secret"] = "a-test-signing-secret-long-enough-for-hmac-sha256",
                ["Frontend:Url"] = FrontendUrl,
            })
            .Build();

        var sender = new CapturingSender();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);
        services.Replace(ServiceDescriptor.Singleton(substitute ?? sender));

        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        var scope = provider.CreateScope();

        return (scope.ServiceProvider.GetRequiredService<IEmailService>(), sender, scope);
    }

    private static Member Person(string first, string email) =>
        Member.Create(first, "Rai", email, null, TheGroup.Id);

    private static Deposit MonthlyDeposit(string? notes = null) =>
        Deposit.Create(Guid.NewGuid(), TheGroup.Id, 5_000m, 5, 2082, DateOnly.FromDateTime(Today),
            DepositType.MonthlyDeposit, notes, Guid.NewGuid());

    private static Loan PendingLoan() =>
        Loan.Create(Guid.NewGuid(), "Member", TheGroup.Id, 50_000m, 10m, Today, null, null);

    [Fact]
    public async Task ADepositEmailCarriesNoButtonAndNoUrl()
    {
        var (service, sender, scope) = Harness();
        using (scope)
        {
            await service.SendDepositVerifiedAsync(
                [new EmailRecipient("ram@example.com", "Ram Rai")], TheGroup, MonthlyDeposit(), Person("Sita", "sita@example.com"));
        }

        var (_, _, subject, html, text) = Assert.Single(sender.Sent);

        Assert.Contains("Bhadra 2082", subject);
        // Every email except the loan request tells the reader what happened rather than
        // asking them to go and do something, so none of them carries a link.
        Assert.DoesNotContain("<a href", html);
        Assert.DoesNotContain(FrontendUrl, html);
        Assert.DoesNotContain("http", text);
    }

    [Theory]
    [InlineData("Sunrise Savers")]
    [InlineData("Himal & Co <Lalitpur>")]
    public async Task EveryEmailSignsOffWithTheGroupsOwnName(string groupName)
    {
        var group = Group.Create(groupName, "SUN");

        var (service, sender, scope) = Harness();
        using (scope)
        {
            // Two different emails, to show the sign-off is the layout's doing and not
            // something each one remembers to add.
            await service.SendDepositVerifiedAsync(
                [new EmailRecipient("ram@example.com", "Ram Rai")], group, MonthlyDeposit(), Person("Sita", "sita@example.com"));
            await service.SendExpenseVerifiedAsync(
                [new EmailRecipient("ram@example.com", "Ram Rai")], group,
                Expense.Create(group.Id, 500m, "Stationery", "Ledger books", Today, Guid.NewGuid()));
        }

        Assert.Equal(2, sender.Sent.Count);
        // The name in the inbox list is the group's too — it is the first thing a reader sees,
        // and the one that tells them which group is writing before they open anything.
        Assert.All(sender.Sent, sent => Assert.Equal(groupName, sent.FromName));

        foreach (var (_, _, _, html, text) in sender.Sent)
        {
            // A person in several groups is receiving mail from each of them, so the sign-off
            // has to say whose books they are reading about.
            Assert.Contains($"— {WebUtility.HtmlEncode(groupName)}", html);
            Assert.Contains($"— {groupName}", text);
            Assert.DoesNotContain("— HamroSavings", html);
        }
    }

    [Fact]
    public async Task TheLoanRequestLinkIsMadeAbsoluteAgainstTheFrontend()
    {
        var loan = PendingLoan();

        var (service, sender, scope) = Harness();
        using (scope)
        {
            await service.SendLoanRequestedAsync(
                [new EmailRecipient("sita@example.com", "Sita Rai")], TheGroup, loan, Person("Ram", "ram@example.com"));
        }

        var (_, _, _, html, text) = Assert.Single(sender.Sent);

        Assert.Contains($"{FrontendUrl}/loans/{loan.Id}", html);
        Assert.Contains("Approve or decline", html);
        // The plain-text alternative needs the URL spelled out; there is no button to click.
        Assert.Contains($"{FrontendUrl}/loans/{loan.Id}", text);
    }

    [Fact]
    public async Task TheSubjectAndTheGroupGetDifferentOpeningsInTheSameSend()
    {
        var depositor = Person("Sita", "sita@example.com");

        var (service, sender, scope) = Harness();
        using (scope)
        {
            await service.SendDepositVerifiedAsync(
                [new EmailRecipient("ram@example.com", "Ram Rai"), new EmailRecipient("sita@example.com", "Sita Rai")], TheGroup, MonthlyDeposit(), depositor);
        }

        var toRam = sender.Sent.Single(s => s.Recipient == "ram@example.com");
        var toSita = sender.Sent.Single(s => s.Recipient == "sita@example.com");

        Assert.Contains("Sita Rai has made a monthly deposit", toRam.Html);
        Assert.Contains("Your monthly deposit", toSita.Html);
        Assert.Contains("Your monthly deposit", toSita.Subject);

        // The figures are the same event and do not change with the reader.
        Assert.Contains("Rs. 5,000.00", toRam.Html);
        Assert.Contains("Rs. 5,000.00", toSita.Html);
    }

    [Fact]
    public async Task AMembersOwnWordsAreNotTreatedAsMarkup()
    {
        var (service, sender, scope) = Harness();
        using (scope)
        {
            await service.SendDepositVerifiedAsync(
                [new EmailRecipient("ram@example.com", "Ram Rai")], TheGroup, MonthlyDeposit(notes: "5,000 & 2,000 <urgent>"), Person("Sita", "sita@example.com"));
        }

        var (_, _, _, html, _) = Assert.Single(sender.Sent);

        // A note written plainly must arrive looking the way it was typed.
        Assert.Contains("5,000 &amp; 2,000 &lt;urgent&gt;", html);
        Assert.DoesNotContain("<urgent>", html);
    }

    [Fact]
    public async Task ADisbursementForcedByAnAdminSaysSo()
    {
        var loan = PendingLoan();
        loan.ForceDisbursement(Guid.NewGuid(), Today, new CashInHand(1_000_000m), new LoanVoteTally(0, 2));

        var (service, sender, scope) = Harness();
        using (scope)
        {
            await service.SendLoanDisbursedAsync(
                [new EmailRecipient("sita@example.com", "Sita Rai")], TheGroup, loan, Person("Ram", "ram@example.com"));
        }

        var (_, _, _, html, _) = Assert.Single(sender.Sent);

        // Spending the group's money without their vote is the whole point of telling them.
        Assert.Contains("without a completed vote", html);
    }

    [Fact]
    public async Task TheInviteIsBuiltFromTheTokenAndLooksLikeEveryOtherEmail()
    {
        var token = Guid.NewGuid();

        var (service, sender, scope) = Harness();
        using (scope)
        {
            await service.SendMemberInviteAsync(new EmailRecipient("sita@example.com", "Sita Rai"), TheGroup, token);
        }

        var (to, _, subject, html, text) = Assert.Single(sender.Sent);

        Assert.Equal("sita@example.com", to);
        Assert.Equal("You've been invited to Sunrise Savers", subject);
        Assert.Equal("Sunrise Savers", sender.Sent[0].FromName);
        // The caller hands over a token; the frontend's address is this layer's business.
        Assert.Contains($"{FrontendUrl}/signup?token={token}", html);
        Assert.Contains($"{FrontendUrl}/signup?token={token}", text);
        Assert.Contains("Create my account", html);
        Assert.Contains("Hello Sita Rai", html);
        // An invite has no figures, so it must not carry the empty shell of a table.
        Assert.DoesNotContain("<table", html);
    }

    [Fact]
    public async Task AFailedInviteReachesTheCallerRatherThanBeingSwallowed()
    {
        var (service, _, scope) = Harness(new FlakySender("sita@example.com"));

        using (scope)
        {
            // Resending an invite exists to send this one email. A group notification is logged
            // and dropped because nobody is waiting on it; this one has an admin waiting to be
            // told whether the person was actually asked.
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.SendMemberInviteAsync(new EmailRecipient("sita@example.com", "Sita Rai"), TheGroup, Guid.NewGuid()));
        }
    }

    [Fact]
    public async Task OneUnreachableAddressDoesNotCostTheRestOfTheGroupTheirEmail()
    {
        var flaky = new FlakySender("ram@example.com");
        var (service, _, scope) = Harness(flaky);

        using (scope)
        {
            // The caller is a domain event handler running after the transaction committed.
            // It has nothing useful to do about a bad mailbox, so a delivery failure must
            // never surface to it — and must not abandon the recipients still in the list.
            await service.SendDepositVerifiedAsync(
                [
                    new EmailRecipient("sita@example.com", "Sita Rai"),
                    new EmailRecipient("ram@example.com", "Ram Rai"),
                    new EmailRecipient("gopal@example.com", "Gopal Rai"),
                ], TheGroup, MonthlyDeposit(), Person("Bina", "bina@example.com"));
        }

        Assert.Equal(["sita@example.com", "gopal@example.com"], flaky.Delivered);
    }
}
