namespace WalletApp.Models;

public class Category
{
    public string CategoryId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Expense"; // Income, Expense
    public string Icon { get; set; } = "💰";

    public static Category FromRow(IList<object> row) => new()
    {
        CategoryId = row.Count > 0 ? row[0]?.ToString() ?? "" : "",
        Name = row.Count > 1 ? row[1]?.ToString() ?? "" : "",
        Type = row.Count > 2 ? row[2]?.ToString() ?? "Expense" : "Expense",
        Icon = row.Count > 3 ? row[3]?.ToString() ?? "💰" : "💰"
    };

    public IList<object> ToRow() => new List<object> { CategoryId, Name, Type, Icon };
}