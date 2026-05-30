namespace WalletApp.Models;

public class RecurringTransaction
{
    public string RecurringId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Expense"; // Income, Expense
    public string AccountId { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public decimal Amount { get; set; } = 0;
    public string Frequency { get; set; } = "Monthly"; // Daily, Weekly, Monthly, Yearly
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime NextDueDate { get; set; } = DateTime.Today;
    public string Note { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static RecurringTransaction FromRow(IList<object> row) => new()
    {
        RecurringId = row.Count > 0 ? row[0]?.ToString() ?? "" : "",
        Name = row.Count > 1 ? row[1]?.ToString() ?? "" : "",
        Type = row.Count > 2 ? row[2]?.ToString() ?? "Expense" : "Expense",
        AccountId = row.Count > 3 ? row[3]?.ToString() ?? "" : "",
        CategoryId = row.Count > 4 ? row[4]?.ToString() ?? "" : "",
        Amount = row.Count > 5 ? decimal.TryParse(row[5]?.ToString(), out var a) ? a : 0 : 0,
        Frequency = row.Count > 6 ? row[6]?.ToString() ?? "Monthly" : "Monthly",
        StartDate = row.Count > 7 ? DateTime.TryParse(row[7]?.ToString(), out var s) ? s : DateTime.Today : DateTime.Today,
        NextDueDate = row.Count > 8 ? DateTime.TryParse(row[8]?.ToString(), out var n) ? n : DateTime.Today : DateTime.Today,
        Note = row.Count > 9 ? row[9]?.ToString() ?? "" : "",
        IsActive = row.Count > 10 ? row[10]?.ToString()?.ToLower() == "true" : true,
        CreatedAt = row.Count > 11 ? DateTime.TryParse(row[11]?.ToString(), out var c) ? c : DateTime.Now : DateTime.Now
    };

    public IList<object> ToRow() => new List<object>
    {
        RecurringId,
        Name,
        Type,
        AccountId,
        CategoryId,
        Amount.ToString("F2"),
        Frequency,
        StartDate.ToString("yyyy-MM-dd"),
        NextDueDate.ToString("yyyy-MM-dd"),
        Note,
        IsActive.ToString(),
        CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
    };

    // Calculate next due date after processing
    public DateTime CalculateNextDueDate() => Frequency switch
    {
        "Daily" => NextDueDate.AddDays(1),
        "Weekly" => NextDueDate.AddDays(7),
        "Monthly" => NextDueDate.AddMonths(1),
        "Yearly" => NextDueDate.AddYears(1),
        _ => NextDueDate.AddMonths(1)
    };

    public string FrequencyLabel => Frequency switch
    {
        "Daily" => "Every day",
        "Weekly" => "Every week",
        "Monthly" => "Every month",
        "Yearly" => "Every year",
        _ => Frequency
    };
}