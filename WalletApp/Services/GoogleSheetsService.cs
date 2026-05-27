using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using WalletApp.Models;

namespace WalletApp.Services;

public class GoogleSheetsService
{
    private SheetsService? _service;
    private readonly string _spreadsheetId;
    private readonly string _clientId;

    public bool IsAuthenticated => _service != null;

    public GoogleSheetsService(IConfiguration config)
    {
        _spreadsheetId = config["Google:SpreadsheetId"] ?? "";
        _clientId = config["Google:ClientId"] ?? "";
    }

    public void SetAccessToken(string accessToken)
    {
        var credential = GoogleCredential.FromAccessToken(accessToken);
        _service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "WalletApp"
        });
    }

    // ── Generic helpers ─────────────────────────────────────────────

    private async Task<IList<IList<object>>> ReadRange(string range)
    {
        if (_service == null) throw new InvalidOperationException("Not authenticated");
        var request = _service.Spreadsheets.Values.Get(_spreadsheetId, range);
        var response = await request.ExecuteAsync();
        return response.Values ?? new List<IList<object>>();
    }

    private async Task AppendRow(string range, IList<object> row)
    {
        if (_service == null) throw new InvalidOperationException("Not authenticated");
        var body = new ValueRange { Values = new List<IList<object>> { row } };
        var request = _service.Spreadsheets.Values.Append(body, _spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync();
    }

    private async Task UpdateRow(string range, IList<object> row)
    {
        if (_service == null) throw new InvalidOperationException("Not authenticated");
        var body = new ValueRange { Values = new List<IList<object>> { row } };
        var request = _service.Spreadsheets.Values.Update(body, _spreadsheetId, range);
        request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
        await request.ExecuteAsync();
    }

    // ── Accounts ────────────────────────────────────────────────────

    public async Task<List<Account>> GetAccountsAsync()
    {
        var rows = await ReadRange("Accounts!A2:F");
        return rows.Select(Account.FromRow).Where(a => !string.IsNullOrEmpty(a.AccountId)).ToList();
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

    // ── Transactions ─────────────────────────────────────────────────

    public async Task<List<Transaction>> GetTransactionsAsync(
        DateTime? from = null, DateTime? to = null, string? accountId = null)
    {
        var rows = await ReadRange("Transactions!A2:H");
        var txns = rows.Select(Transaction.FromRow).Where(t => !string.IsNullOrEmpty(t.TransactionId)).ToList();

        if (from.HasValue) txns = txns.Where(t => t.Date >= from.Value).ToList();
        if (to.HasValue) txns = txns.Where(t => t.Date <= to.Value).ToList();
        if (!string.IsNullOrEmpty(accountId)) txns = txns.Where(t => t.AccountId == accountId).ToList();

        return txns.OrderByDescending(t => t.Date).ToList();
    }

    public async Task AddTransactionAsync(Transaction txn)
    {
        await AppendRow("Transactions!A:H", txn.ToRow());
        // Update account balance
        var accounts = await GetAccountsAsync();
        var account = accounts.FirstOrDefault(a => a.AccountId == txn.AccountId);
        if (account != null)
        {
            var delta = txn.Type == "Income" ? txn.Amount : -txn.Amount;
            var newBalance = account.Balance + delta;
            await UpdateAccountBalanceAsync(account.AccountId, newBalance);
        }
    }

    // ── Transfers ────────────────────────────────────────────────────

    public async Task<List<Transfer>> GetTransfersAsync()
    {
        var rows = await ReadRange("Transfers!A2:G");
        return rows.Select(Transfer.FromRow).Where(t => !string.IsNullOrEmpty(t.TransferId))
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

    // ── Categories ───────────────────────────────────────────────────

    public async Task<List<Category>> GetCategoriesAsync()
    {
        var rows = await ReadRange("Categories!A2:D");
        return rows.Select(Category.FromRow).Where(c => !string.IsNullOrEmpty(c.CategoryId)).ToList();
    }

    public async Task AddCategoryAsync(Category category)
        => await AppendRow("Categories!A:D", category.ToRow());

    // ── Dashboard ────────────────────────────────────────────────────

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
}