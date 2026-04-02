using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Loans;

namespace HamroSavings.Application.Loans.GetLoans;

public sealed record GetLoansQuery(
    Guid? GroupId = null,
    Guid? BorrowerId = null,
    LoanStatus? Status = null) : IQuery<List<LoanResponse>>;
