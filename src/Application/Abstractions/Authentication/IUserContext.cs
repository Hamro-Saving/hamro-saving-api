using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }
    Guid? MemberId { get; }
    UserRole Role { get; }
    Guid? GroupId { get; }
    MembershipType? MembershipType { get; }
    bool IsSuperAdmin { get; }
    bool IsAdmin { get; }
    bool IsMember { get; }
}
