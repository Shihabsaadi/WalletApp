namespace WalletApp.Models;

public class Transfer
{
    public string TransferId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public DateTime Date { get; set; } = DateTime.Now;
    public string FromAccountId { get; set; } = "";
    public string ToAccountId { get; set; } = "";
    public decimal Amount { get; set; } = 0;
    public string Note { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public static Transfer FromRow(IList<object> row) => new()
    {
        TransferId = row.Count > 0 ? row[0]?.ToString() ?? "" : "",
        Date = row.Count > 1 ? DateTime.TryParse(row[1]?.ToString(), out var d) ? d : DateTime.Now : DateTime.Now,
        FromAccountId = row.Count > 2 ? row[2]?.ToString() ?? "" : "",
        ToAccountId = row.Count > 3 ? row[3]?.ToString() ?? "" : "",
        Amount = row.Count > 4 ? decimal.TryParse(row[4]?.ToString(), out var a) ? a : 0 : 0,
        Note = row.Count > 5 ? row[5]?.ToString() ?? "" : "",
        CreatedAt = row.Count > 6 ? DateTime.TryParse(row[6]?.ToString(), out var c) ? c : DateTime.Now : DateTime.Now
    };

    public IList<object> ToRow() => new List<object>
        { TransferId, Date.ToString("yyyy-MM-dd"), FromAccountId, ToAccountId,
          Amount.ToString("F2"), Note, CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") };
}