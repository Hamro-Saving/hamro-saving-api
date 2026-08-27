using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.ListPayments;

public sealed record ListLoanPaymentsQuery(
    Guid? GroupId = null,
    Guid? BorrowerId = null,
    bool? IsVerified = null) : IQuery<List<LoanPaymentListItemResponse>>;
