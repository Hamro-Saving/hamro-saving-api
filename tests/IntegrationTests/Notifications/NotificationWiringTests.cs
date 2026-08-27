using HamroSavings.Application;
using HamroSavings.Domain.Finance;
using HamroSavings.Domain.Loans;
using HamroSavings.Domain.Savings;
using HamroSavings.Infrastructure;
using HamroSavings.Infrastructure.Database;
using HamroSavings.SharedKernel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Notifications;

/// <summary>
/// Handlers are found by assembly scan and dispatched by reflection, so nothing at compile
/// time says they are registered — one could look correct and never run. Resolved here the
/// same way the processor does it.
/// </summary>
public class NotificationWiringTests
{
    private static ServiceProvider BuildContainer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:HamroSavingsDb"] = "Host=localhost;Database=test;Username=test;Password=test",
                ["Jwt:Secret"] = "a-test-signing-secret-long-enough-for-hmac-sha256",
                ["Jwt:Issuer"] = "HamroSavings",
                ["Jwt:Audience"] = "HamroSavings",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        // The host normally supplies this; several infrastructure services read from it directly.
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);

        // ValidateScopes, but deliberately not ValidateOnBuild: AddInfrastructure brings in the
        // ASP.NET authentication stack, whose own registrations need the routing services that
        // only a WebApplication builds. Scope validation is the part that matters here anyway —
        // a handler resolved into the processor's scope must not depend on anything singleton
        // that captures a DbContext.
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    public static TheoryData<Type> NotifiedEvents =>
    [
        typeof(DepositRecordedDomainEvent),
        typeof(DepositVerifiedDomainEvent),
        typeof(LoanRequestedDomainEvent),
        typeof(LoanVoteSettledDomainEvent),
        typeof(LoanDisbursedDomainEvent),
        typeof(LoanPaymentRecordedDomainEvent),
        typeof(LoanPaymentVerifiedDomainEvent),
        typeof(ExpenseVerifiedDomainEvent),
        typeof(FixedDepositVerifiedDomainEvent),
        typeof(FixedDepositWithdrawalVerifiedDomainEvent),
        typeof(OtherIncomingFundVerifiedDomainEvent),
    ];

    [Theory]
    [MemberData(nameof(NotifiedEvents))]
    public void EveryNotifiedEventHasAHandlerTheProcessorCanResolve(Type eventType)
    {
        using var provider = BuildContainer();
        using var scope = provider.CreateScope();

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        Assert.NotEmpty(scope.ServiceProvider.GetServices(handlerType));
    }

    public static TheoryData<Type> EventsWithNoHandler =>
    [
        typeof(ExpenseRecordedDomainEvent),
        typeof(FixedDepositRecordedDomainEvent),
        typeof(FixedDepositWithdrawalRecordedDomainEvent),
        typeof(OtherIncomingFundRecordedDomainEvent),
    ];

    [Theory]
    [MemberData(nameof(EventsWithNoHandler))]
    public void TheFinanceRecordsAnnounceNothingUntilTheyAreVerified(Type eventType)
    {
        using var provider = BuildContainer();
        using var scope = provider.CreateScope();

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);

        // Recording one of these is not news to the group — it commits nothing and is not on
        // the books. The event is still raised so a handler can be added later, but nothing
        // should be listening to it today.
        Assert.Empty(scope.ServiceProvider.GetServices(handlerType));
    }

    [Fact]
    public void TheDbContextCanBeBuiltWithItsPublisherInjected()
    {
        using var provider = BuildContainer();
        using var scope = provider.CreateScope();

        // Constructing it is the assertion: the context now takes IDomainEventPublisher, and
        // EF resolves that from the container rather than from DbContextOptions.
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<HamroSavingsDbContext>());
    }

    [Fact]
    public void TheQueueIsSharedAcrossScopes()
    {
        using var provider = BuildContainer();
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        // Events published by one request have to reach the one processor draining them, so
        // the publisher must be the same instance everywhere.
        Assert.Same(
            first.ServiceProvider.GetRequiredService<IDomainEventPublisher>(),
            second.ServiceProvider.GetRequiredService<IDomainEventPublisher>());
    }
}
