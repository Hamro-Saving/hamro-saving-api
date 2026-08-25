namespace HamroSavings.Domain.Savings;

/// <summary>
/// A deposit's month and year are recorded in Bikram Sambat, which is the calendar the
/// group actually keeps its books in. Only the month names are needed here: the values
/// are already BS, so nothing is converted.
/// </summary>
public static class BikramSambat
{
    public static readonly string[] Months =
    [
        "Baishakh", "Jestha", "Ashadh", "Shrawan",
        "Bhadra", "Ashwin", "Kartik", "Mangsir",
        "Poush", "Magh", "Falgun", "Chaitra",
    ];

    public static string MonthName(int month) =>
        month >= 1 && month <= 12 ? Months[month - 1] : month.ToString();

    /// <summary>A period as people say it: "Bhadra 2082".</summary>
    public static string Period(int month, int year) => $"{MonthName(month)} {year}";
}
