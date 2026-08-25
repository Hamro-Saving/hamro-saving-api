using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

/// <summary>
/// Interest paid by a member who joined after the group started saving, to catch up with
/// what the earlier members' money has been earning in the meantime.
///
/// It is income to the group, not savings: unlike a deposit, the group does not owe it
/// back to the person who paid it. That is why it is recorded here rather than as a
/// deposit — a deposit would credit member savings and overstate what is owed out.
/// </summary>
public sealed class LateJoinerInterest : Entity
{
    public Guid Id { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid MemberId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidDate { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private LateJoinerInterest() { }

    public static Result<LateJoinerInterest> Record(
        Guid groupId,
        Guid memberId,
        decimal amount,
        DateTime paidDate,
        string? notes,
        Guid recordedById)
    {
        if (amount <= 0)
            return Result.Failure<LateJoinerInterest>(LateJoinerInterestErrors.AmountNotPositive);

        return Result.Success(new LateJoinerInterest
        {
            Id = Guid.CreateVersion7(),
            GroupId = groupId,
            MemberId = memberId,
            Amount = amount,
            PaidDate = paidDate,
            Notes = notes,
            RecordedById = recordedById,
            CreatedAt = DateTime.UtcNow
        });
    }

    public void Update(decimal amount, DateTime paidDate, string? notes)
    {
        Amount = amount;
        PaidDate = paidDate;
        Notes = notes;
    }
}
