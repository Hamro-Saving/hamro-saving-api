using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Loans.CastVote;

/// <summary>
/// A single vote on a pending loan. <paramref name="IsApproved"/> false is a decline.
/// </summary>
public sealed record CastLoanVoteCommand(Guid LoanId, bool IsApproved) : ICommand;
