using HamroSavings.Application.Abstractions.Data;
using HamroSavings.Application.Abstractions.Messaging;
using HamroSavings.Domain.Users;
using HamroSavings.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace HamroSavings.Application.Auth.GetSignupInfo;

internal sealed class GetSignupInfoQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetSignupInfoQuery, SignupInfoResponse>
{
    public async Task<Result<SignupInfoResponse>> Handle(GetSignupInfoQuery query, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .Where(u => u.InviteToken == query.Token && u.InviteTokenExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return Result.Failure<SignupInfoResponse>(UserErrors.InviteTokenInvalid);

        if (user.MemberId is null)
            return Result.Failure<SignupInfoResponse>(UserErrors.InviteTokenInvalid);

        var member = await dbContext.Members
            .FindAsync([user.MemberId.Value], cancellationToken);

        if (member is null)
            return Result.Failure<SignupInfoResponse>(UserErrors.InviteTokenInvalid);

        return Result.Success(new SignupInfoResponse(
            member.Email!,
            member.FirstName,
            member.LastName!,
            member.FullName));
    }
}
