using HamroSavings.SharedKernel;

namespace HamroSavings.Domain.Loans;

public sealed class LoanApproval : Entity
{
    public Guid Id { get; private set; }
    public Guid LoanId { get; private set; }
    public Guid ApproverId { get; private set; }
    public DateTime ApprovedAt { get; private set; }

    private LoanApproval() { }

    public static LoanApproval Create(Guid loanId, Guid approverId) =>
        new LoanApproval
        {
            Id = Guid.CreateVersion7(),
            LoanId = loanId,
            ApproverId = approverId,
            ApprovedAt = DateTime.UtcNow
        };
}
