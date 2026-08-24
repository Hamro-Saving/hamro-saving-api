using HamroSavings.Domain.Members;
using HamroSavings.Domain.Users;

namespace HamroSavings.Application.Abstractions.Authentication;

public interface ITokenProvider
{
    /// <summary>
    /// Mints a token for <paramref name="user"/> acting inside <paramref name="activeMember"/>'s group.
    /// <paramref name="memberships"/> carries every group the person belongs to so the client can
    /// offer a switcher without a round trip.
    /// </summary>
    string Create(User user, Member? activeMember, IReadOnlyList<MembershipClaim> memberships);
}
