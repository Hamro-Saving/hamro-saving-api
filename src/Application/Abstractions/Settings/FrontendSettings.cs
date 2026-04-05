namespace HamroSavings.Application.Abstractions.Settings;

public sealed class FrontendSettings
{
    public const string SectionName = "Frontend";
    public string Url { get; set; } = "http://localhost:5173";
}
