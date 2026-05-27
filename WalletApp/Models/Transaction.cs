namespace WalletApp.Models;

public class Transaction
{
    public string TransactionId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public DateTime Date { get; set; } = DateTime.Now;
    public string Type { get; set; } = "Expense"; // Income, Expense
    public string AccountId { get; set; } = "";
    public string CategoryId { get; set; } = "";
    public decimal Amount { get; set; } = 0;
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static Transaction FromRow(IList<object> row) => new()
    {
        TransactionId = row.Count > 0 ? row[0]?.ToString() ?? "" : "",
        Date = row.Count > 1 ? DateTime.TryParse(row[1]?.ToString(), out var d) ? d : DateTime.Now : DateTime.Now,
        Type = row.Count > 2 ? row[2]?.ToString() ?? "Expense" : "Expense",
        AccountId = row.Count > 3 ? row[3]?.ToString() ?? "" : "",
        CategoryId = row.Count > 4 ? row[4]?.ToString() ?? "" : "",
        Amount = row.Count > 5 ? decimal.TryParse(row[5]?.ToString(), out var a) ? a : 0 : 0,
        Note = row.Count > 6 ? row[6]?.ToString() ?? "" : "",
        CreatedAt = row.Count > 7 ? DateTime.TryParse(row[7]?.ToString(), out var c) ? c : DateTime.Now : DateTime.Now
    };

    public IList<object> ToRow() => new List<object>
        { TransactionId, Date.ToString("yyyy-MM-dd"), Type, AccountId, CategoryId,
          Amount.ToString("F2"), Note, CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") };
}