using HamroSavings.Application.Abstractions.Messaging;

namespace HamroSavings.Application.Auth.GetSignupInfo;

public sealed record GetSignupInfoQuery(Guid Token) : IQuery<SignupInfoResponse>;

public sealed record SignupInfoResponse(
    string Email,
    string FirstName,
    string LastName,
    string FullName);
