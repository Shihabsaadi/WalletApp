using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WalletApp.Models;

namespace WalletApp.Services;

public class ExportService
{
    public byte[] ExportToExcel(List<Transaction> transactions,
                                List<Account> accounts,
                                List<Category> categories)
    {
        var accMap = accounts.ToDictionary(a => a.AccountId, a => a.Name);
        var catMap = categories.ToDictionary(c => c.CategoryId, c => c.Name);

        using var wb = new XLWorkbook();

        // Transactions sheet
        var ws = wb.Worksheets.Add("Transactions");
        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Type";
        ws.Cell(1, 3).Value = "Account";
        ws.Cell(1, 4).Value = "Category";
        ws.Cell(1, 5).Value = "Amount";
        ws.Cell(1, 6).Value = "Note";

        var header = ws.Range(1, 1, 1, 6);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
        header.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < transactions.Count; i++)
        {
            var t = transactions[i];
            var row = i + 2;
            ws.Cell(row, 1).Value = t.Date.ToString("yyyy-MM-dd");
            ws.Cell(row, 2).Value = t.Type;
            ws.Cell(row, 3).Value = accMap.GetValueOrDefault(t.AccountId, t.AccountId);
            ws.Cell(row, 4).Value = catMap.GetValueOrDefault(t.CategoryId, t.CategoryId);
            ws.Cell(row, 5).Value = (double)t.Amount;
            ws.Cell(row, 6).Value = t.Note;

            if (t.Type == "Income")
                ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#16A34A");
            else
                ws.Cell(row, 5).Style.Font.FontColor = XLColor.FromHtml("#DC2626");
        }

        ws.Columns().AdjustToContents();

        // Accounts sheet
        var ws2 = wb.Worksheets.Add("Accounts");
        ws2.Cell(1, 1).Value = "Account";
        ws2.Cell(1, 2).Value = "Type";
        ws2.Cell(1, 3).Value = "Balance";
        var h2 = ws2.Range(1, 1, 1, 3);
        h2.Style.Font.Bold = true;
        h2.Style.Fill.BackgroundColor = XLColor.FromHtml("#4F46E5");
        h2.Style.Font.FontColor = XLColor.White;

        for (int i = 0; i < accounts.Count; i++)
        {
            var a = accounts[i];
            ws2.Cell(i + 2, 1).Value = a.Name;
            ws2.Cell(i + 2, 2).Value = a.Type;
            ws2.Cell(i + 2, 3).Value = (double)a.Balance;
        }
        ws2.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportToPdf(List<Transaction> transactions,
                               List<Account> accounts,
                               List<Category> categories)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var accMap = accounts.ToDictionary(a => a.AccountId, a => a.Name);
        var catMap = categories.ToDictionary(c => c.CategoryId, c => c.Name);
        var totalIncome = transactions.Where(t => t.Type == "Income").Sum(t => t.Amount);
        var totalExpense = transactions.Where(t => t.Type == "Expense").Sum(t => t.Amount);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(t => t.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("Wallet Report").FontSize(20).Bold().FontColor("#4F46E5");
                    col.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy}").FontSize(9).FontColor("#6B7280");
                    col.Item().Height(10);
                });

                page.Content().Column(col =>
                {
                    // Summary row
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(8).Column(c =>
                        {
                            c.Item().Text("Total Income").FontSize(9).FontColor("#6B7280");
                            c.Item().Text($"৳{totalIncome:N2}").FontSize(14).Bold().FontColor("#16A34A");
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(8).Column(c =>
                        {
                            c.Item().Text("Total Expense").FontSize(9).FontColor("#6B7280");
                            c.Item().Text($"৳{totalExpense:N2}").FontSize(14).Bold().FontColor("#DC2626");
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(8).Column(c =>
                        {
                            c.Item().Text("Net Savings").FontSize(9).FontColor("#6B7280");
                            c.Item().Text($"৳{totalIncome - totalExpense:N2}").FontSize(14).Bold().FontColor("#4F46E5");
                        });
                    });

                    col.Item().Height(16);

                    // Transactions table
                    col.Item().Text("Transactions").FontSize(13).Bold();
                    col.Item().Height(6);
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Date", "Type", "Account", "Category", "Amount" })
                                header.Cell().Background("#4F46E5").Padding(5)
                                      .Text(h).FontColor("#FFFFFF").Bold().FontSize(9);
                        });

                        foreach (var t in transactions.Take(100))
                        {
                            table.Cell().BorderBottom(0.5f).BorderColor("#F3F4F6").Padding(5)
                                 .Text(t.Date.ToString("dd MMM")).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor("#F3F4F6").Padding(5)
                                 .Text(t.Type).FontColor(t.Type == "Income" ? "#16A34A" : "#DC2626").FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor("#F3F4F6").Padding(5)
                                 .Text(accMap.GetValueOrDefault(t.AccountId, "")).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor("#F3F4F6").Padding(5)
                                 .Text(catMap.GetValueOrDefault(t.CategoryId, "")).FontSize(9);
                            table.Cell().BorderBottom(0.5f).BorderColor("#F3F4F6").Padding(5)
                                 .Text($"৳{t.Amount:N2}").FontColor(t.Type == "Income" ? "#16A34A" : "#DC2626").FontSize(9);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ").FontSize(8).FontColor("#9CA3AF");
                    x.CurrentPageNumber().FontSize(8).FontColor("#9CA3AF");
                });
            });
        });

        return doc.GeneratePdf();
    }
}