namespace HamroSavings.SharedKernel;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
