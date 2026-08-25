using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Finance;

/// <summary>
/// Money coming into the group that is not a member's savings and not a loan repayment —
/// interest paid by a member who joined after the group started saving, a fine, a refund,
/// a contribution from outside.
///
/// It is income, not savings: unlike a deposit, the group does not owe it back to whoever
/// paid it. That is why it is recorded here rather than as a deposit, which would credit
/// member savings and overstate what is owed out.
///
/// Because the category is broad, the remark is what says which kind of income this was.
/// It is required for that reason: a row here without one is unidentifiable later.
/// </summary>
public sealed class OtherIncomingFund : Entity
{
    public Guid Id { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid MemberId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaidDate { get; private set; }

    /// <summary>What this money was for. Required — see the note on the class.</summary>
    public string Remarks { get; private set; } = string.Empty;

    public Guid RecordedById { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private OtherIncomingFund() { }

    public static Result<OtherIncomingFund> Record(
        Guid groupId,
        Guid memberId,
        decimal amount,
        DateTime paidDate,
        string remarks,
        Guid recordedById)
    {
        if (amount <= 0)
            return Result.Failure<OtherIncomingFund>(OtherIncomingFundErrors.AmountNotPositive);

        if (string.IsNullOrWhiteSpace(remarks))
            return Result.Failure<OtherIncomingFund>(OtherIncomingFundErrors.RemarksRequired);

        return Result.Success(new OtherIncomingFund
        {
            Id = Guid.CreateVersion7(),
            GroupId = groupId,
            MemberId = memberId,
            Amount = amount,
            PaidDate = paidDate,
            Remarks = remarks.Trim(),
            RecordedById = recordedById,
            CreatedAt = DateTime.UtcNow
        });
    }

    public Result Update(decimal amount, DateTime paidDate, string remarks)
    {
        if (amount <= 0)
            return Result.Failure(OtherIncomingFundErrors.AmountNotPositive);

        if (string.IsNullOrWhiteSpace(remarks))
            return Result.Failure(OtherIncomingFundErrors.RemarksRequired);

        Amount = amount;
        PaidDate = paidDate;
        Remarks = remarks.Trim();
        return Result.Success();
    }
}
