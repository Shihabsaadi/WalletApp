namespace WalletApp.Models;

public class DashboardStats
{
    public decimal TotalBalance { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalSavings => TotalIncome - TotalExpense;
    public List<Account> Accounts { get; set; } = new();
    public List<(string Category, decimal Amount)> TopExpenses { get; set; } = new();
    public List<(string Date, decimal Income, decimal Expense)> ChartData { get; set; } = new();
}