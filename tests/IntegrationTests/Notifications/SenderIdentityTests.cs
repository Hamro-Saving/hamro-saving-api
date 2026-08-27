using HamroSavings.Infrastructure.Email;

namespace IntegrationTests.Notifications;

/// <summary>
/// The From line is all a person reads before deciding whether to open it: which group is
/// writing, at an address the sending server can answer for.
/// </summary>
public class SenderIdentityTests
{
    [Fact]
    public void TheSenderIsTheGroup()
    {
        // Mail from a group reads as being from that group, with nothing appended.
        Assert.Equal("Sunrise Savers", SenderIdentity.DisplayName("Sunrise Savers"));
    }

    [Fact]
    public void TheGroupGoesInTheLocalPartAndTheDomainIsLeftAlone()
    {
        // The domain is what a receiver checks SPF and DMARC against, so it stays exactly as
        // configured — a savings group has a name, not a domain of its own.
        Assert.Equal("noreply+sunrise-savers@hamrosavings.com",
            SenderIdentity.Address("Sunrise Savers", "noreply@hamrosavings.com"));
    }

    [Theory]
    [InlineData("Himal & Co", "noreply+himal-co@hamrosavings.com")]
    [InlineData("  Spaced  Out  ", "noreply+spaced-out@hamrosavings.com")]
    [InlineData("Group #1 (2082)", "noreply+group-1-2082@hamrosavings.com")]
    public void PunctuationCollapsesRatherThanProducingAnUnsendableAddress(string groupName, string expected)
    {
        Assert.Equal(expected, SenderIdentity.Address(groupName, "noreply@hamrosavings.com"));
    }

    [Fact]
    public void ANameWithNothingUsableInItKeepsThePlainAddress()
    {
        // Group names are often written in Devanagari. Slugging one leaves nothing behind, and
        // an address like "noreply+@..." is not a valid one — so the plain address stands.
        Assert.Equal("noreply@hamrosavings.com",
            SenderIdentity.Address("बचत समूह", "noreply@hamrosavings.com"));
        Assert.Equal("noreply@hamrosavings.com",
            SenderIdentity.Address("", "noreply@hamrosavings.com"));
    }

    [Fact]
    public void AVeryLongNameIsTrimmedToFitTheLocalPart()
    {
        var address = SenderIdentity.Address(new string('a', 120), "noreply@hamrosavings.com");

        // Local parts are capped at 64 characters, and an over-long one is rejected outright.
        Assert.True(address.IndexOf('@') <= 64, address);
        Assert.DoesNotContain("+-", address);
    }

    [Fact]
    public void AMisconfiguredBaseAddressIsPassedThroughUntouched()
    {
        // Nothing useful can be done with it here, and mangling it further would only make the
        // eventual failure harder to read.
        Assert.Equal("not-an-address", SenderIdentity.Address("Sunrise Savers", "not-an-address"));
    }
}
