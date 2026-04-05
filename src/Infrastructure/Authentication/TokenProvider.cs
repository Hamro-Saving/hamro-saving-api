using HamroSavings.Application.Abstractions.Authentication;
using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace HamroSavings.Infrastructure.Authentication;

internal sealed class TokenProvider(IConfiguration configuration) : ITokenProvider
{
    public string Create(User user, Member? member = null)
    {
        string secret = configuration["Jwt:Secret"]!;
        string issuer = configuration["Jwt:Issuer"]!;
        string audience = configuration["Jwt:Audience"]!;
        int expirationMinutes = int.Parse(configuration["Jwt:ExpirationInMinutes"] ?? "1440");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        // Role comes from User; name comes from linked Member (SuperAdmin has no Member)
        var role = user.Role.ToString();
        var groupId = member?.GroupId.ToString() ?? string.Empty;
        var memberId = member?.Id.ToString() ?? string.Empty;
        var membershipType = member?.MembershipType.ToString() ?? string.Empty;
        var firstName = member?.FirstName ?? string.Empty;
        var lastName = member?.LastName ?? string.Empty;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim("GroupId", groupId),
            new Claim("firstName", firstName),
            new Claim("lastName", lastName),
            new Claim("MemberId", memberId),
            new Claim("MembershipType", membershipType),
        };

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
