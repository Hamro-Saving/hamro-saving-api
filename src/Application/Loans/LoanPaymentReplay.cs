using HamroSavings.Domain.Loans;
using HamroSavings.SharedKernel;

namespace HamroSavings.Application.Loans;

/// <summary>
/// Re-applies a loan's payments from the moment it was disbursed.
///
/// Each payment settles the interest that had run up to its date, and every payment after it
/// was worked out from the position it left behind. Correcting or removing one therefore
/// invalidates all of them, and winding the loan back and applying what remains — in the order
/// it was paid — is the only way to arrive at figures that still add up.
/// </summary>
internal static class LoanPaymentReplay
{
    /// <summary>
    /// Whether a correction to <paramref name="payment"/> would disturb something already
    /// posted. Replaying restates every payment, so a verified one after this in the running
    /// order could have its posted split rewritten while its ledger entries stayed put.
    /// Payments before it are recomputed from unchanged inputs and land where they were.
    /// </summary>
    public static bool HasVerifiedPaymentAfter(LoanPayment payment, IEnumerable<LoanPayment> payments) =>
        payments.Any(p => p.IsVerified && p.Id != payment.Id && ComesAfter(p, payment));

    /// <param name="payments">Every payment the loan should have once the correction is made.</param>
    public static Result Apply(Loan loan, IEnumerable<LoanPayment> payments)
    {
        var rewound = loan.RewindToDisbursement();
        if (rewound.IsFailure) return rewound;

        foreach (var payment in payments.OrderBy(p => p.PaidDate).ThenBy(p => p.CreatedAt))
        {
            var allocation = loan.RecordPayment(payment.PaidDate, payment.PrincipalAmount, payment.InterestAmount);
            if (allocation.IsFailure) return Result.Failure(allocation.Error);

            payment.Restate(allocation.Value);
        }

        return Result.Success();
    }

    /// <summary>The order payments are applied in: by the day paid, then by when it was entered.</summary>
    private static bool ComesAfter(LoanPayment payment, LoanPayment other) =>
        payment.PaidDate != other.PaidDate
            ? payment.PaidDate > other.PaidDate
            : payment.CreatedAt > other.CreatedAt;
}
