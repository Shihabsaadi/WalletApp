namespace WalletApp.Models;

public class Account
{
    public string AccountId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Cash"; // Cash, Bank, Savings, Salary, Investment
    public decimal Balance { get; set; } = 0;
    public string Currency { get; set; } = "BDT";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static Account FromRow(IList<object> row) => new()
    {
        AccountId = row.Count > 0 ? row[0]?.ToString() ?? "" : "",
        Name = row.Count > 1 ? row[1]?.ToString() ?? "" : "",
        Type = row.Count > 2 ? row[2]?.ToString() ?? "Cash" : "Cash",
        Balance = row.Count > 3 ? decimal.TryParse(row[3]?.ToString(), out var b) ? b : 0 : 0,
        Currency = row.Count > 4 ? row[4]?.ToString() ?? "BDT" : "BDT",
        CreatedAt = row.Count > 5 ? DateTime.TryParse(row[5]?.ToString(), out var d) ? d : DateTime.Now : DateTime.Now
    };

    public IList<object> ToRow() => new List<object>
        { AccountId, Name, Type, Balance.ToString("F2"), Currency, CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") };
}