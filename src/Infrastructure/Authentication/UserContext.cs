using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Users;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HamroSavings.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId =>
        Guid.Parse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    public UserRole Role
    {
        get
        {
            var roleStr = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role)
                ?? throw new InvalidOperationException("Role claim not found.");
            return Enum.Parse<UserRole>(roleStr);
        }
    }

    public Guid? GroupId
    {
        get
        {
            var groupIdStr = httpContextAccessor.HttpContext?.User.FindFirstValue("GroupId");
            if (string.IsNullOrEmpty(groupIdStr)) return null;
            return Guid.TryParse(groupIdStr, out var id) ? id : null;
        }
    }

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
    public bool IsAdmin => Role == UserRole.Admin || IsSuperAdmin;
    public bool IsMember => Role == UserRole.Member;
}
