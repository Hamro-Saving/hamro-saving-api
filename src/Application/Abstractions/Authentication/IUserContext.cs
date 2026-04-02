using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }
    UserRole Role { get; }
    Guid? GroupId { get; }
    bool IsSuperAdmin { get; }
    bool IsAdmin { get; }
    bool IsMember { get; }
}
