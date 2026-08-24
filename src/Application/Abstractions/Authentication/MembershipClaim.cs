using HamroSavings.Domain.Members;

namespace HamroSavings.Application.Abstractions.Authentication;

/// <summary>One group a person belongs to, as carried in the JWT.</summary>
public sealed record MembershipClaim(
    Guid GroupId,
    string GroupName,
    Guid MemberId,
    GroupRole GroupRole);
