using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

/// <summary>
/// One member's vote on a pending loan. <see cref="IsApproved"/> distinguishes an
/// approval from a decline; a member gets a single vote per loan either way.
/// </summary>
public sealed class LoanApproval : Entity
{
    public Guid Id { get; private set; }
    public Guid LoanId { get; private set; }
    public Guid ApproverId { get; private set; }
    public bool IsApproved { get; private set; }
    public DateTime ApprovedAt { get; private set; }

    private LoanApproval() { }

    public static LoanApproval Create(Guid loanId, Guid approverId, bool isApproved) =>
        new LoanApproval
        {
            Id = Guid.CreateVersion7(),
            LoanId = loanId,
            ApproverId = approverId,
            IsApproved = isApproved,
            ApprovedAt = DateTime.UtcNow
        };
}
