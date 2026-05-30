using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Apis.Sheets.v4.Data;
using WalletApp.Models;

namespace WalletApp.Services;

public class ExportService
{
    // ── Excel export (unchanged) ─────────────────────────────────────
    public byte[] ExportToExcel(List<Transaction> transactions,
                                List<Account> accounts,
                                List<Models.Category> categories,
                                List<Transfer> transfers)
    {
        var accMap = accounts.ToDictionary(a => a.AccountId, a => a.Name);
        var catMap = categories.ToDictionary(c => c.CategoryId, c => c.Name);

        using var wb = new XLWorkbook();

        // Sheet 1 — Transactions
        var ws = wb.Worksheets.Add("Transactions");
        var txnHeaders = new[] { "Date", "Type", "Account", "Category", "Amount", "Note" };
        for (int i = 0; i < txnHeaders.Length; i++) ws.Cell(1, i + 1).Value = txnHeaders[i];
        var txnHeader = ws.Range(1, 1, 1, txnHeaders.Length);
        txnHeader.Style.Font.Bold = true;
        txnHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
        txnHeader.Style.Font.FontColor = XLColor.White;
        for (int i = 0; i < transactions.Count; i++)
        {
            var t = transactions[i]; var row = i + 2;
            ws.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd");
            ws.Cell(row, 2).Value = t.Type;
            ws.Cell(row, 3).Value = accMap.GetValueOrDefault(t.AccountId, t.AccountId);
            ws.Cell(row, 4).Value = catMap.GetValueOrDefault(t.CategoryId, t.CategoryId);
            ws.Cell(row, 5).Value = (double)t.Amount;
            ws.Cell(row, 6).Value = t.Note;
            ws.Cell(row, 5).Style.Font.FontColor =
                t.Type == "Income" ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");
            if (row % 2 == 0) ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");
        }
        var tl = transactions.Count + 2;
        ws.Cell(tl, 4).Value = "Total Income"; ws.Cell(tl, 5).Value = (double)transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
        ws.Cell(tl + 1, 4).Value = "Total Expense"; ws.Cell(tl + 1, 5).Value = (double)transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);
        ws.Cell(tl + 2, 4).Value = "Net Savings"; ws.Cell(tl + 2, 5).Value = (double)(transactions.Where(t => t.Type == "Income").Sum(t => t.Amount) - transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount));
        foreach (var r in new[] { tl, tl + 1, tl + 2 }) { ws.Cell(r, 4).Style.Font.Bold = true; ws.Cell(r, 5).Style.Font.Bold = true; }
        ws.Cell(tl, 5).Style.Font.FontColor = XLColor.FromHtml("#16A34A");
        ws.Cell(tl + 1, 5).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
        ws.Cell(tl + 2, 5).Style.Font.FontColor = XLColor.FromHtml("#4F46E5");
        ws.Columns().AdjustToContents();

        // Sheet 2 — Transfers
        var ws2 = wb.Worksheets.Add("Transfers");
        var trHeaders = new[] { "Date", "From Account", "To Account", "Amount", "Note" };
        for (int i = 0; i < trHeaders.Length; i++) ws2.Cell(1, i + 1).Value = trHeaders[i];
        var trHeader = ws2.Range(1, 1, 1, trHeaders.Length);
        trHeader.Style.Font.Bold = true;
        trHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#7C3AED");
        trHeader.Style.Font.FontColor = XLColor.White;
        for (int i = 0; i < transfers.Count; i++)
        {
            var t = transfers[i]; var row = i + 2;
            ws2.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd");
            ws2.Cell(row, 2).Value = accMap.GetValueOrDefault(t.FromAccountId, t.FromAccountId);
            ws2.Cell(row, 3).Value = accMap.GetValueOrDefault(t.ToAccountId, t.ToAccountId);
            ws2.Cell(row, 4).Value = (double)t.Amount;
            ws2.Cell(row, 5).Value = t.Note;
            ws2.Cell(row, 4).Style.Font.FontColor = XLColor.FromHtml("#7C3AED");
            if (row % 2 == 0) ws2.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");
        }
        var trl = transfers.Count + 2;
        ws2.Cell(trl, 3).Value = "Total Transferred";
        ws2.Cell(trl, 4).Value = (double)transfers.Sum(t => t.Amount);
        ws2.Cell(trl, 3).Style.Font.Bold = true; ws2.Cell(trl, 4).Style.Font.Bold = true;
        ws2.Cell(trl, 4).Style.Font.FontColor = XLColor.FromHtml("#7C3AED");
        ws2.Columns().AdjustToContents();

        // Sheet 3 — Accounts
        var ws3 = wb.Worksheets.Add("Accounts");
        var accHeaders = new[] { "Account Name", "Type", "Balance", "Currency" };
        for (int i = 0; i < accHeaders.Length; i++) ws3.Cell(1, i + 1).Value = accHeaders[i];
        var accHeader = ws3.Range(1, 1, 1, accHeaders.Length);
        accHeader.Style.Font.Bold = true;
        accHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F6E56");
        accHeader.Style.Font.FontColor = XLColor.White;
        for (int i = 0; i < accounts.Count; i++)
        {
            var a = accounts[i]; var row = i + 2;
            ws3.Cell(row, 1).Value = a.Name;
            ws3.Cell(row, 2).Value = a.Type;
            ws3.Cell(row, 3).Value = (double)a.Balance;
            ws3.Cell(row, 4).Value = a.Currency;
            ws3.Cell(row, 3).Style.Font.FontColor =
                a.Balance >= 0 ? XLColor.FromHtml("#16A34A") : XLColor.FromHtml("#DC2626");
            if (row % 2 == 0) ws3.Range(row, 1, row, 4).Style.Fill.BackgroundColor = XLColor.FromHtml("#F9FAFB");
        }
        var al = accounts.Count + 2;
        ws3.Cell(al, 2).Value = "Total Balance";
        ws3.Cell(al, 3).Value = (double)accounts.Sum(a => a.Balance);
        ws3.Cell(al, 2).Style.Font.Bold = true; ws3.Cell(al, 3).Style.Font.Bold = true;
        ws3.Cell(al, 3).Style.Font.FontColor = XLColor.FromHtml("#4F46E5");
        ws3.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ── PDF export — generates styled HTML, browser prints it to PDF ─
    public string GeneratePdfHtml(List<Transaction> transactions,
                                List<Account> accounts,
                                List<Models.Category> categories,
                                List<Transfer> transfers)
    {
        var accMap = accounts.ToDictionary(a => a.AccountId, a => a.Name);
        var catMap = categories.ToDictionary(c => c.CategoryId, c => c.Name);
        var totalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);
        var totalTransferred = transfers.Sum(t => t.Amount);
        var netSavings = totalIncome - totalExpense;

        var txnRows = string.Join("", transactions.Select((t, i) =>
            $$"""
        <tr style="background:{{(i % 2 == 0 ? "#fff" : "#F9FAFB")}}">
            <td>{{t.Date:dd MMM yyyy}}</td>
            <td><span style="background:{{(t.Type == "Income" ? "#DCFCE7" : "#FEE2E2")}};color:{{(t.Type == "Income" ? "#16A34A" : "#DC2626")}};padding:2px 8px;border-radius:99px;font-size:11px">{{t.Type}}</span></td>
            <td>{{accMap.GetValueOrDefault(t.AccountId, "")}}</td>
            <td>{{catMap.GetValueOrDefault(t.CategoryId, "")}}</td>
            <td style="color:{{(t.Type == "Income" ? "#16A34A" : "#DC2626")}};font-weight:600">৳{{t.Amount:N2}}</td>
            <td style="color:#6B7280">{{t.Note}}</td>
        </tr>
        """));

        var transferRows = string.Join("", transfers.Select((t, i) =>
            $$"""
        <tr style="background:{{(i % 2 == 0 ? "#fff" : "#F9FAFB")}}">
            <td>{{t.Date:dd MMM yyyy}}</td>
            <td style="color:#DC2626;font-weight:500">{{accMap.GetValueOrDefault(t.FromAccountId, "")}}</td>
            <td style="color:#16A34A;font-weight:500">{{accMap.GetValueOrDefault(t.ToAccountId, "")}}</td>
            <td style="color:#7C3AED;font-weight:600">৳{{t.Amount:N2}}</td>
            <td style="color:#6B7280">{{t.Note}}</td>
        </tr>
        """));

        var accountRows = string.Join("", accounts.Select((a, i) =>
            $$"""
        <tr style="background:{{(i % 2 == 0 ? "#fff" : "#F9FAFB")}}">
            <td style="font-weight:500">{{a.Name}}</td>
            <td>{{a.Type}}</td>
            <td style="color:{{(a.Balance >= 0 ? "#16A34A" : "#DC2626")}};font-weight:600">৳{{a.Balance:N2}}</td>
            <td>{{a.Currency}}</td>
        </tr>
        """));

        // $$""" means: single { } = literal CSS brace, {{ expression }} = C# interpolation
        return $$"""
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8"/>
        <title>Wallet Report — {{DateTime.Now:dd MMM yyyy}}</title>
        <style>
            * { box-sizing: border-box; margin: 0; padding: 0; }
            body { font-family: Arial, sans-serif; font-size: 13px; color: #111827; padding: 32px; }
            h1 { font-size: 22px; color: #4F46E5; margin-bottom: 4px; }
            h2 { font-size: 15px; font-weight: 600; margin: 28px 0 10px; color: #374151; border-bottom: 2px solid #E5E7EB; padding-bottom: 6px; }
            .sub { font-size: 12px; color: #6B7280; margin-bottom: 20px; }
            .summary { display: flex; gap: 12px; margin-bottom: 28px; flex-wrap: wrap; }
            .summary-card { flex: 1; min-width: 120px; border-radius: 10px; padding: 14px 16px; }
            .s-income   { background: #DCFCE7; }
            .s-expense  { background: #FEE2E2; }
            .s-savings  { background: #EEF2FF; }
            .s-transfer { background: #F5F3FF; }
            .summary-card .label  { font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: .04em; margin-bottom: 4px; }
            .summary-card .amount { font-size: 20px; font-weight: 700; }
            .s-income   .label  { color: #15803D; } .s-income   .amount { color: #16A34A; }
            .s-expense  .label  { color: #B91C1C; } .s-expense  .amount { color: #DC2626; }
            .s-savings  .label  { color: #4338CA; } .s-savings  .amount { color: #4F46E5; }
            .s-transfer .label  { color: #6D28D9; } .s-transfer .amount { color: #7C3AED; }
            table  { width: 100%; border-collapse: collapse; margin-bottom: 8px; }
            thead tr { background: #4F46E5; color: #fff; }
            th { padding: 8px 10px; text-align: left; font-size: 12px; font-weight: 600; }
            td { padding: 8px 10px; border-bottom: 1px solid #F3F4F6; font-size: 12px; }
            .thead-purple thead tr { background: #7C3AED; }
            .thead-green  thead tr { background: #0F6E56; }
            tfoot tr { background: #F3F4F6; font-weight: 700; }
            tfoot td { border-bottom: none; }
            .footer { margin-top: 32px; text-align: center; font-size: 11px; color: #9CA3AF; }
            @media print {
                body { padding: 16px; }
                .no-print { display: none; }
                h2 { page-break-before: auto; }
            }
        </style>
    </head>
    <body>
        <h1>💰 Wallet Report</h1>
        <div class="sub">Generated on {{DateTime.Now:dddd, dd MMMM yyyy}}</div>

        <div class="summary">
            <div class="summary-card s-income">
                <div class="label">Total Income</div>
                <div class="amount">৳{{totalIncome:N2}}</div>
            </div>
            <div class="summary-card s-expense">
                <div class="label">Total Expense</div>
                <div class="amount">৳{{totalExpense:N2}}</div>
            </div>
            <div class="summary-card s-savings">
                <div class="label">Net Savings</div>
                <div class="amount">৳{{netSavings:N2}}</div>
            </div>
            <div class="summary-card s-transfer">
                <div class="label">Transferred</div>
                <div class="amount">৳{{totalTransferred:N2}}</div>
            </div>
        </div>

        <h2>Transactions ({{transactions.Count}})</h2>
        <table>
            <thead>
                <tr><th>Date</th><th>Type</th><th>Account</th><th>Category</th><th>Amount</th><th>Note</th></tr>
            </thead>
            <tbody>{{txnRows}}</tbody>
            <tfoot>
                <tr>
                    <td colspan="4" style="text-align:right;padding-right:12px">Total Income</td>
                    <td style="color:#16A34A">৳{{totalIncome:N2}}</td><td></td>
                </tr>
                <tr>
                    <td colspan="4" style="text-align:right;padding-right:12px">Total Expense</td>
                    <td style="color:#DC2626">৳{{totalExpense:N2}}</td><td></td>
                </tr>
                <tr style="background:#EEF2FF">
                    <td colspan="4" style="text-align:right;padding-right:12px">Net Savings</td>
                    <td style="color:#4F46E5">৳{{netSavings:N2}}</td><td></td>
                </tr>
            </tfoot>
        </table>

        <h2>Transfers ({{transfers.Count}})</h2>
        <table class="thead-purple">
            <thead>
                <tr><th>Date</th><th>From</th><th>To</th><th>Amount</th><th>Note</th></tr>
            </thead>
            <tbody>{{transferRows}}</tbody>
            <tfoot>
                <tr style="background:#F5F3FF">
                    <td colspan="3" style="text-align:right;padding-right:12px">Total Transferred</td>
                    <td style="color:#7C3AED">৳{{totalTransferred:N2}}</td><td></td>
                </tr>
            </tfoot>
        </table>

        <h2>Accounts ({{accounts.Count}})</h2>
        <table class="thead-green">
            <thead>
                <tr><th>Account</th><th>Type</th><th>Balance</th><th>Currency</th></tr>
            </thead>
            <tbody>{{accountRows}}</tbody>
            <tfoot>
                <tr style="background:#F0FDF4">
                    <td colspan="2" style="text-align:right;padding-right:12px">Total Balance</td>
                    <td style="color:#4F46E5">৳{{accounts.Sum(a => a.Balance):N2}}</td><td></td>
                </tr>
            </tfoot>
        </table>

        <div class="footer">My Wallet — Personal Finance Tracker</div>
    </body>
    </html>
    """;
    }
}