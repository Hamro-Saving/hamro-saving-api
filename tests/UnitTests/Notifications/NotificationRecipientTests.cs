using HamroSavings.Application.Abstractions.Email;
using HamroSavings.Application.Notifications;
using HamroSavings.Domain.Members;

namespace UnitTests.Notifications;

/// <summary>
/// Pinned rather than left to the shape of a LINQ chain: a member left out does not know
/// their group lent money, and a non-member let in is reading books that are not theirs.
/// </summary>
public class NotificationRecipientTests
{
    private static readonly Guid TheGroup = Guid.NewGuid();
    private static readonly Guid AnotherGroup = Guid.NewGuid();

    private static Member Person(
        string name,
        GroupRole role = GroupRole.Member,
        Guid? group = null,
        Guid? userId = null,
        string? email = null,
        bool active = true)
    {
        var member = role == GroupRole.NonMember
            ? Member.CreateNonMember(name, email ?? $"{name}@example.com", null, null, group ?? TheGroup)
            : Member.Create(name, "Rai", email ?? $"{name}@example.com", null, group ?? TheGroup, role);

        if (userId is { } id) member.LinkUser(id);
        if (!active) member.Deactivate();
        return member;
    }

    private static List<string> NamesOf(IEnumerable<Member> members) =>
        [.. members.Select(m => m.FirstName).Order()];

    private static IQueryable<Member> Participants(params Member[] roster) =>
        NotificationRecipients.Participants(roster.AsQueryable(), TheGroup);

    [Fact]
    public void TheAudienceIsTheGroupsActiveReachableParticipants()
    {
        var roster = Participants(
            Person("Sita"),
            Person("Ram", GroupRole.Admin),
            Person("Hari", GroupRole.NonMember),
            Person("Gita", active: false),
            Person("Bina", email: ""),
            Person("Kamal", group: AnotherGroup));

        // An admin is a member with extra privileges and hears what a member hears. A
        // non-member, someone who has left, someone unreachable and someone in a different
        // group are all outside it.
        Assert.Equal(["Ram", "Sita"], NamesOf(roster));
    }

    [Fact]
    public void OnlyAdminsAreAskedToVerify()
    {
        var roster = Participants(Person("Sita"), Person("Ram", GroupRole.Admin));

        Assert.Equal(["Ram"], NamesOf(roster.AdminsOnly()));
    }

    [Fact]
    public void TheSubjectOfARequestIsNotAnAudienceForIt()
    {
        var borrower = Person("Sita");
        var roster = Participants(borrower, Person("Ram", GroupRole.Admin));

        Assert.Equal(["Ram"], NamesOf(roster.ExceptMember(borrower.Id)));
    }

    [Fact]
    public void TheVerifyingAdminIsLeftOutButEveryOtherAdminIsNot()
    {
        var verifierUserId = Guid.NewGuid();
        var roster = Participants(
            Person("Ram", GroupRole.Admin, userId: verifierUserId),
            Person("Gopal", GroupRole.Admin, userId: Guid.NewGuid()),
            Person("Sita", userId: Guid.NewGuid()));

        // The one person who already knows is the one who just did it.
        Assert.Equal(["Gopal", "Sita"], NamesOf(roster.ExceptUser(verifierUserId)));
    }

    [Fact]
    public void AMemberWithNoLoginStillGetsTheirMail()
    {
        var roster = Participants(
            Person("Sita", userId: null),
            Person("Ram", GroupRole.Admin, userId: Guid.NewGuid()));

        // A member with no login of their own cannot be the person who verified anything, so
        // comparing their absent user id must never drop them from the group's mail.
        Assert.Contains("Sita", NamesOf(roster.ExceptUser(Guid.NewGuid())));
    }

    [Fact]
    public void ExcludingNobodyKeepsEveryone()
    {
        var roster = Participants(Person("Sita"), Person("Ram", GroupRole.Admin));

        Assert.Equal(["Ram", "Sita"], NamesOf(roster.ExceptUser(null)));
        Assert.Equal(["Ram", "Sita"], NamesOf(roster.ExceptMember(null)));
    }

    [Fact]
    public void ANonMemberBorrowerHearsAboutTheirOwnLoan()
    {
        var outsider = Person("Hari", GroupRole.NonMember);
        List<EmailRecipient> group = [new("sita@example.com", "Sita Rai")];

        var withBorrower = group.IncludingBorrower(outsider);

        // They are not part of the group's audience and never hear about anyone else's
        // money — but this loan is theirs.
        Assert.Contains(withBorrower, r => r.Email == "hari@example.com");
    }

    [Fact]
    public void AMemberBorrowerIsNotMailedTwice()
    {
        var borrower = Person("Sita", email: "sita@example.com");
        List<EmailRecipient> group = [new("sita@example.com", "Sita Rai"), new("ram@example.com", "Ram Rai")];

        Assert.Equal(2, group.IncludingBorrower(borrower).Count);
    }

    [Fact]
    public void ABorrowerWithNoEmailIsSimplyNotAdded()
    {
        var outsider = Member.CreateNonMember("Hari Thapa", null, null, null, TheGroup);
        List<EmailRecipient> group = [new("sita@example.com", "Sita Rai")];

        Assert.Single(group.IncludingBorrower(outsider));
    }

    [Fact]
    public void AnAdminWhoVerifiedAPaymentOnTheirOwnLoanIsNotAddedBack()
    {
        var verifierUserId = Guid.NewGuid();
        var borrower = Person("Ram", GroupRole.Admin, userId: verifierUserId, email: "ram@example.com");
        List<EmailRecipient> group = [new("sita@example.com", "Sita Rai")];

        // They were deliberately excluded a moment ago for being the person who did it;
        // being the borrower as well does not undo that.
        Assert.Single(group.IncludingBorrower(borrower, exceptUserId: verifierUserId));
    }
}
