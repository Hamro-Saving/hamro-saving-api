using HamroSavings.Domain.Users;

namespace UnitTests.Users;

/// <summary>
/// A reset link is a credential: anyone holding it can take the account. So it is short-lived,
/// spent on first use, kept apart from the invite token — which answers a different question,
/// whether an account has ever been claimed — and not handed out again the moment it is asked
/// for a second time.
/// </summary>
public class PasswordResetTests
{
    private static readonly DateTime Now = new(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc);

    private static User Active()
    {
        var user = User.CreateMember("sita@example.com", "old-hash");
        user.AcceptInvite("old-hash");
        return user;
    }

    [Fact]
    public void AResetLinkExpiresLongBeforeAnInviteDoes()
    {
        var user = Active();

        var issued = user.GeneratePasswordResetToken(Now);

        Assert.True(issued.IsSuccess);
        // An invite waits for someone to get around to joining; a reset answers a request
        // made a minute ago, so it is measured in an hour rather than days.
        Assert.Equal(Now.AddHours(1), user.PasswordResetTokenExpiresAt);
    }

    [Fact]
    public void SettingThePasswordSpendsTheToken()
    {
        var user = Active();
        user.GeneratePasswordResetToken(Now);

        user.ResetPassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
        // Nothing to look up on a second attempt, so one link sets one password.
        Assert.Null(user.PasswordResetToken);
        Assert.Null(user.PasswordResetTokenExpiresAt);
    }

    // ---------------------------------------------------------------- Who may ask

    [Fact]
    public void AnAccountThatWasNeverClaimedIsGivenNoResetLink()
    {
        // Invited, never signed up. The invite is what sets the first password; a reset link
        // would be a second, unasked-for way into an account nobody has proved they own.
        var user = User.CreateMember("bibek@example.com", "placeholder-hash");
        var invite = user.GenerateInviteToken();

        var issued = user.GeneratePasswordResetToken(Now);

        Assert.True(issued.IsFailure);
        Assert.Equal(UserErrors.NotActive, issued.Error);
        Assert.Null(user.PasswordResetToken);
        // And the invite it is still waiting on is left untouched.
        Assert.Equal(invite, user.InviteToken);
    }

    [Fact]
    public void ADisabledAccountIsGivenNoResetLink()
    {
        // How someone leaves: taken out of their last group, their login goes with it. A
        // reset link would hand back the access that act removed.
        var user = Active();
        user.Deactivate();

        var issued = user.GeneratePasswordResetToken(Now);

        Assert.True(issued.IsFailure);
        Assert.Equal(UserErrors.NotActive, issued.Error);
        Assert.Null(user.PasswordResetToken);
    }

    [Fact]
    public void DisablingAnAccountKillsAResetLinkAlreadyInFlight()
    {
        // Asked for a reset, then removed from the group before clicking it. The link is in
        // their mailbox and must not outlast the decision to close the account.
        var user = Active();
        user.GeneratePasswordResetToken(Now);

        user.Deactivate();

        Assert.Null(user.PasswordResetToken);
        Assert.Null(user.PasswordResetTokenExpiresAt);
    }

    [Fact]
    public void ARestoredAccountCanResetAgain()
    {
        // Put back into a group, the login is reinstated — and so is the ordinary way to
        // recover it. Nothing about the refusal is permanent.
        var user = Active();
        user.Deactivate();

        user.Activate();

        Assert.True(user.GeneratePasswordResetToken(Now.AddMinutes(5)).IsSuccess);
    }

    // ---------------------------------------------------------------- How often

    [Fact]
    public void AskingAgainStraightAwaySendsNothing()
    {
        // The flood case: many callers, each asking once for the same person. Counted against
        // the account rather than the caller, all but the first are refused.
        var user = Active();
        user.GeneratePasswordResetToken(Now);

        var again = user.GeneratePasswordResetToken(Now.AddSeconds(30));

        Assert.True(again.IsFailure);
        Assert.Equal(UserErrors.ResetAskedTooRecently, again.Error);
    }

    [Fact]
    public void ARefusedSecondAskLeavesTheFirstLinkWorking()
    {
        // The person may well be holding the first email. Refusing the second must not
        // invalidate the link they are about to click.
        var user = Active();
        var first = user.GeneratePasswordResetToken(Now).Value;

        user.GeneratePasswordResetToken(Now.AddSeconds(30));

        Assert.Equal(first, user.PasswordResetToken);
        Assert.Equal(Now.AddHours(1), user.PasswordResetTokenExpiresAt);
    }

    [Fact]
    public void AskingAgainAfterTheCooldownSendsAFreshLink()
    {
        // Someone who genuinely lost the first email is not locked out, only slowed down.
        var user = Active();
        var first = user.GeneratePasswordResetToken(Now).Value;

        var later = Now.Add(User.ResetRequestCooldown).AddSeconds(1);
        var second = user.GeneratePasswordResetToken(later);

        Assert.True(second.IsSuccess);
        Assert.NotEqual(first, second.Value);
        // And only the newer one is live, so the older email stops working.
        Assert.Equal(second.Value, user.PasswordResetToken);
    }

    [Fact]
    public void TheCooldownCountsFromTheEmailNotFromTheToken()
    {
        // Using the link clears the token. If the cooldown were read off the token, that
        // would reopen the flood to anyone who could get one spent.
        var user = Active();
        user.GeneratePasswordResetToken(Now);
        user.ResetPassword("new-hash");

        var again = user.GeneratePasswordResetToken(Now.AddSeconds(30));

        Assert.True(again.IsFailure);
        Assert.Equal(UserErrors.ResetAskedTooRecently, again.Error);
    }
}
