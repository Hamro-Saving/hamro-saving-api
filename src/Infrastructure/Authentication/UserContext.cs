using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HamroSavings.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid UserId =>
        Guid.Parse(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    public Guid? MemberId
    {
        get
        {
            var memberIdStr = httpContextAccessor.HttpContext?.User.FindFirstValue("MemberId");
            if (string.IsNullOrEmpty(memberIdStr)) return null;
            return Guid.TryParse(memberIdStr, out var id) ? id : null;
        }
    }

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

    public MembershipType? MembershipType
    {
        get
        {
            var val = httpContextAccessor.HttpContext?.User.FindFirstValue("MembershipType");
            if (string.IsNullOrEmpty(val)) return null;
            return Enum.TryParse<MembershipType>(val, out var mt) ? mt : null;
        }
    }

    public bool IsSuperAdmin => Role == UserRole.SuperAdmin;
    public bool IsAdmin => Role == UserRole.Admin || IsSuperAdmin;
    public bool IsMember => Role == UserRole.Member;
}
