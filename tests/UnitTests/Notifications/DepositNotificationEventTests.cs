using HamroSavings.Domain.Savings;

namespace UnitTests.Notifications;

/// <summary>
/// Two audiences, two moments. Keeping them distinct is what stops the group being told about
/// a figure that is still liable to be corrected.
/// </summary>
public class DepositNotificationEventTests
{
    private static Deposit MonthlyDeposit() =>
        Deposit.Create(
            memberId: Guid.NewGuid(),
            groupId: Guid.NewGuid(),
            amount: 5_000m,
            month: 5,
            year: 2082,
            depositDate: DateOnly.FromDateTime(DateTime.UtcNow),
            type: DepositType.MonthlyDeposit,
            notes: null,
            createdById: Guid.NewGuid());

    [Fact]
    public void RecordingADepositAsksTheAdminsToVerifyIt()
    {
        var deposit = MonthlyDeposit();

        var raised = Assert.Single(deposit.DomainEvents.OfType<DepositRecordedDomainEvent>());
        Assert.Equal(deposit.Id, raised.DepositId);
        Assert.Equal(deposit.MemberId, raised.MemberId);
        Assert.Equal(deposit.GroupId, raised.GroupId);

        // The group hears nothing until it is verified.
        Assert.Empty(deposit.DomainEvents.OfType<DepositVerifiedDomainEvent>());
    }

    [Fact]
    public void VerifyingADepositTellsTheGroup()
    {
        var deposit = MonthlyDeposit();
        deposit.ClearDomainEvents();

        Assert.True(deposit.Verify(Guid.NewGuid()).IsSuccess);

        var raised = Assert.Single(deposit.DomainEvents.OfType<DepositVerifiedDomainEvent>());
        Assert.Equal(deposit.Id, raised.DepositId);
    }

    [Fact]
    public void VerifyingATwiceVerifiedDepositAnnouncesNothing()
    {
        var deposit = MonthlyDeposit();
        deposit.Verify(Guid.NewGuid());
        deposit.ClearDomainEvents();

        Assert.True(deposit.Verify(Guid.NewGuid()).IsFailure);
        Assert.Empty(deposit.DomainEvents);
    }

    [Fact]
    public void CorrectingAnUnverifiedDepositDoesNotReAnnounceIt()
    {
        var deposit = MonthlyDeposit();
        deposit.ClearDomainEvents();

        Assert.True(deposit.Update(6_000m, "corrected", deposit.DepositDate).IsSuccess);
        Assert.Empty(deposit.DomainEvents);
    }
}
