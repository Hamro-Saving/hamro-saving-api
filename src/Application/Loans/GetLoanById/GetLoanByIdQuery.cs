using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Application.Loans.GetLoans;

namespace HamroSavings.Application.Loans.GetLoanById;

public sealed record GetLoanByIdQuery(Guid LoanId) : IQuery<LoanResponse>;
