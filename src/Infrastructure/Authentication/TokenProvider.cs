using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HamroSavings.Infrastructure.Authentication;

internal sealed class TokenProvider(IConfiguration configuration) : ITokenProvider
{
    public string Create(User user, Member? activeMember, IReadOnlyList<MembershipClaim> memberships)
    {
        string secret = configuration["Jwt:Secret"]!;
        string issuer = configuration["Jwt:Issuer"]!;
        string audience = configuration["Jwt:Audience"]!;
        int expirationMinutes = int.Parse(configuration["Jwt:ExpirationInMinutes"] ?? "1440");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(AppClaims.IsSuperAdmin, user.IsSuperAdmin ? "true" : "false"),
            new(AppClaims.GroupId, activeMember?.GroupId.ToString() ?? string.Empty),
            new(AppClaims.MemberId, activeMember?.Id.ToString() ?? string.Empty),
            new(AppClaims.GroupRole, activeMember?.GroupRole.ToString() ?? string.Empty),
            new(AppClaims.FirstName, activeMember?.FirstName ?? string.Empty),
            new(AppClaims.LastName, activeMember?.LastName ?? string.Empty),
            new(AppClaims.Memberships, JsonSerializer.Serialize(memberships.Select(m => new
            {
                groupId = m.GroupId,
                groupName = m.GroupName,
                memberId = m.MemberId,
                groupRole = m.GroupRole.ToString()
            })), JsonClaimValueTypes.JsonArray)
        };

        // Role is emitted once per axis, so "platform superadmin who is also a group admin" is
        // expressible and RequireRole keeps working.
        if (user.IsSuperAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));

        if (activeMember is not null)
            claims.Add(new Claim(ClaimTypes.Role, activeMember.GroupRole.ToString()));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(tokenDescriptor);
    }
}
