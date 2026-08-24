using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Members;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;

namespace HamroSavings.Infrastructure.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid UserId =>
        Guid.Parse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("User ID claim not found."));

    public bool IsSuperAdmin =>
        string.Equals(Principal?.FindFirstValue(AppClaims.IsSuperAdmin), "true", StringComparison.OrdinalIgnoreCase);

    public Guid? ActiveGroupId => ParseGuid(AppClaims.GroupId);

    public Guid? ActiveMemberId => ParseGuid(AppClaims.MemberId);

    public GroupRole? ActiveGroupRole => ParseEnum<GroupRole>(AppClaims.GroupRole);

    public bool IsGroupAdmin => ActiveGroupRole == GroupRole.Admin;

    public bool IsGroupMember => ActiveGroupId is not null;

    public bool ParticipatesInGroup => ActiveGroupRole?.Participates() == true;

    public IReadOnlyList<MembershipClaim> Memberships
    {
        get
        {
            var raw = Principal?.FindFirstValue(AppClaims.Memberships);
            if (string.IsNullOrEmpty(raw)) return [];

            try
            {
                var parsed = JsonSerializer.Deserialize<List<MembershipJson>>(raw);
                return parsed?
                    .Select(m => new MembershipClaim(
                        m.GroupId,
                        m.GroupName ?? string.Empty,
                        m.MemberId,
                        Enum.TryParse<GroupRole>(m.GroupRole, out var gr) ? gr : GroupRole.Member))
                    .ToList() ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }

    private Guid? ParseGuid(string claim)
    {
        var value = Principal?.FindFirstValue(claim);
        if (string.IsNullOrEmpty(value)) return null;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private T? ParseEnum<T>(string claim) where T : struct, Enum
    {
        var value = Principal?.FindFirstValue(claim);
        if (string.IsNullOrEmpty(value)) return null;
        return Enum.TryParse<T>(value, out var parsed) ? parsed : null;
    }

    private sealed record MembershipJson(
        Guid GroupId,
        string? GroupName,
        Guid MemberId,
        string? GroupRole);
}
