using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WalletApp.Models;

namespace WalletApp.Services;

public class GoogleSheetsService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private string _spreadsheetId = "";
    private string _accessToken = "";

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

    public GoogleSheetsService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _config = config;
        _spreadsheetId = config["Google:SpreadsheetId"] ?? "";
    }

    public void SetAccessToken(string accessToken)
    {
        _accessToken = accessToken;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    // ── Base URL ─────────────────────────────────────────────────────

    private string BaseUrl =>
        $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values";

    // ── Generic helpers ───────────────────────────────────────────────

    private async Task<IList<IList<object>>> ReadRange(string range)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(range)}";
        var response = await _http.GetFromJsonAsync<SheetValueRange>(url);
        return response?.Values ?? new List<IList<object>>();
    }

    private async Task AppendRow(string range, IList<object> row)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(range)}:append?valueInputOption=USER_ENTERED";
        var body = new SheetValueRange { Values = new List<IList<object>> { row } };
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _http.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
    }

    private async Task UpdateRow(string range, IList<object> row)
    {
        var url = $"{BaseUrl}/{Uri.EscapeDataString(range)}?valueInputOption=USER_ENTERED";
        var body = new SheetValueRange { Values = new List<IList<object>> { row } };
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    // ── Accounts ──────────────────────────────────────────────────────

    public async Task<List<Account>> GetAccountsAsync()
    {
        var rows = await ReadRange("Accounts!A2:F");
        return rows.Select(Account.FromRow)
                   .Where(a => !string.IsNullOrEmpty(a.AccountId)).ToList();
    }

    public async Task AddAccountAsync(Account account)
        => await AppendRow("Accounts!A:F", account.ToRow());

    public async Task UpdateAccountBalanceAsync(string accountId, decimal newBalance)
    {
        var rows = await ReadRange("Accounts!A2:F");
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Count > 0 && rows[i][0]?.ToString() == accountId)
            {
                var account = Account.FromRow(rows[i]);
                account.Balance = newBalance;
                await UpdateRow($"Accounts!A{i + 2}:F{i + 2}", account.ToRow());
                return;
            }
        }
    }

    // ── Transactions ──────────────────────────────────────────────────

    public async Task<List<Transaction>> GetTransactionsAsync(
        DateTime? from = null, DateTime? to = null, string? accountId = null)
    {
        var rows = await ReadRange("Transactions!A2:H");
        var txns = rows.Select(Transaction.FromRow)
                       .Where(t => !string.IsNullOrEmpty(t.TransactionId)).ToList();

        if (from.HasValue) txns = txns.Where(t => t.Date >= from.Value).ToList();
        if (to.HasValue) txns = txns.Where(t => t.Date <= to.Value).ToList();
        if (!string.IsNullOrEmpty(accountId)) txns = txns.Where(t => t.AccountId == accountId).ToList();

        return txns.OrderByDescending(t => t.Date).ToList();
    }

    public async Task AddTransactionAsync(Transaction txn)
    {
        await AppendRow("Transactions!A:H", txn.ToRow());
        var accounts = await GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.AccountId == txn.AccountId);
        if (account != null)
        {
            var delta = txn.Type == "Income" ? txn.Amount : -txn.Amount;
            var newBalance = account.Balance + delta;
            await UpdateAccountBalanceAsync(account.AccountId, newBalance);
        }
    }

    // ── Transfers ─────────────────────────────────────────────────────

    public async Task<List<Transfer>> GetTransfersAsync()
    {
        var rows = await ReadRange("Transfers!A2:G");
        return rows.Select(Transfer.FromRow)
                   .Where(t => !string.IsNullOrEmpty(t.TransferId))
                   .OrderByDescending(t => t.Date).ToList();
    }

    public async Task AddTransferAsync(Transfer transfer)
    {
        await AppendRow("Transfers!A:G", transfer.ToRow());
        var accounts = await GetAccountsAsync();
        var from = accounts.FirstOrDefault(a => a.AccountId == transfer.FromAccountId);
        var to = accounts.FirstOrDefault(a => a.AccountId == transfer.ToAccountId);
        if (from != null) await UpdateAccountBalanceAsync(from.AccountId, from.Balance - transfer.Amount);
        if (to != null) await UpdateAccountBalanceAsync(to.AccountId, to.Balance + transfer.Amount);
    }

    // ── Categories ────────────────────────────────────────────────────

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var rows = await ReadRange("Categories!A2:D");
        return rows.Select(Category.FromRow)
                   .Where(c => !string.IsNullOrEmpty(c.CategoryId)).ToList();
    }

    public async Task AddCategoryAsync(Category category)
        => await AppendRow("Categories!A:D", category.ToRow());

    // ── Dashboard ─────────────────────────────────────────────────────

    public async Task<DashboardStats> GetDashboardStatsAsync(
        DateTime? from = null, DateTime? to = null, List<string>? accountIds = null)
    {
        var accounts = await GetAccountsAsync();
        var transactions = await GetTransactionsAsync(from, to);
        var categories = await GetCategoriesAsync();

        if (accountIds != null && accountIds.Any())
        {
            accounts = accounts.Where(a => accountIds.Contains(a.AccountId)).ToList();
            transactions = transactions.Where(t => accountIds.Contains(t.AccountId)).ToList();
        }

        var catMap = categories.ToDictionary(c => c.CategoryId, c => c.Name);

        var topExpenses = transactions
            .Where(t => t.Type == "Expense")
            .GroupBy(t => catMap.GetValueOrDefault(t.CategoryId, "Other"))
            .Select(g => (g.Key, g.Sum(t => t.Amount)))
            .OrderByDescending(x => x.Item2)
            .Take(5).ToList();

        var chartData = transactions
            .GroupBy(t => t.Date.ToString("MMM dd"))
            .Select(g => (g.Key,
                g.Where(t => t.Type == "Income").Sum(t => t.Amount),
                g.Where(t => t.Type == "Expense").Sum(t => t.Amount)))
            .OrderBy(x => x.Key).ToList();

        return new DashboardStats
        {
            TotalBalance = accounts.Sum(a => a.Balance),
            TotalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount),
            TotalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount),
            Accounts = accounts,
            TopExpenses = topExpenses,
            ChartData = chartData
        };
    }
    public async Task UpdateAccountAsync(string accountId, string name, string type, decimal balance, string currency)
    {
        var rows = await ReadRange("Accounts!A2:F");
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Count > 0 && rows[i][0]?.ToString() == accountId)
            {
                var updatedRow = new List<object>
            {
                accountId,
                name,
                type,
                balance.ToString("F2"),
                currency,
                rows[i].Count > 5 ? rows[i][5]?.ToString() ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                                  : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
                await UpdateRow($"Accounts!A{i + 2}:F{i + 2}", updatedRow);
                return;
            }
        }
    }

    public async Task DeleteAccountAsync(string accountId)
    {
        // Google Sheets API does not have a direct delete row via values API
        // We clear the row by writing empty strings — the row stays but is blank
        // GetAccountsAsync already filters out empty AccountIds so it won't appear
        var rows = await ReadRange("Accounts!A2:F");
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Count > 0 && rows[i][0]?.ToString() == accountId)
            {
                var emptyRow = new List<object> { "", "", "", "", "", "" };
                await UpdateRow($"Accounts!A{i + 2}:F{i + 2}", emptyRow);
                return;
            }
        }
    }

    public async Task DeleteTransactionAsync(Transaction txn)
    {
        var rows = await ReadRange("Transactions!A2:H");
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Count > 0 && rows[i][0]?.ToString() == txn.TransactionId)
            {
                // Clear the row
                var emptyRow = new List<object> { "", "", "", "", "", "", "", "" };
                await UpdateRow($"Transactions!A{i + 2}:H{i + 2}", emptyRow);

                // Reverse the balance effect on the account
                var accounts = await GetAccountsAsync();
                var account = accounts.FirstOrDefault(a => a.AccountId == txn.AccountId);
                if (account != null)
                {
                    var reversal = txn.Type == "Income" ? -txn.Amount : txn.Amount;
                    var newBalance = account.Balance + reversal;
                    await UpdateAccountBalanceAsync(account.AccountId, newBalance);
                }
                return;
            }
        }
    }
    // ── JSON model for Sheets API response ───────────────────────────

    private class SheetValueRange
    {
        [System.Text.Json.Serialization.JsonPropertyName("values")]
        public List<IList<object>>? Values { get; set; }
    }
}