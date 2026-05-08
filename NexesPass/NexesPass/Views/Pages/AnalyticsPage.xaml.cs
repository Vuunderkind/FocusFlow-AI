using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexesPass.Models;

namespace NexesPass.Views.Pages;

public partial class AnalyticsPage : Page
{
    public AnalyticsPage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var now = DateTime.Now;
        PeriodText.Text = $"Период: {now:MMMM yyyy}";

        var txService = App.TransactionService;
        var txs = await txService.GetRecentTransactionsAsync(count: 200);

        var thisMonth = txs.Where(t =>
            t.CreatedAt.Year == now.Year && t.CreatedAt.Month == now.Month).ToList();

        var userAccountIds = (await App.AccountService.GetUserAccountsAsync()).Select(a => a.Id).ToList();

        decimal income = thisMonth
            .Where(t => t.ToAccountId.HasValue && userAccountIds.Contains(t.ToAccountId.Value)
                && t.Type != TransactionType.Transfer)
            .Sum(t => t.Amount);

        decimal expense = thisMonth
            .Where(t => t.FromAccountId.HasValue && userAccountIds.Contains(t.FromAccountId.Value))
            .Sum(t => t.Amount);

        IncomeText.Text = $"{income:N0} ₽";
        ExpenseText.Text = $"{expense:N0} ₽";

        decimal net = income - expense;
        NetText.Text = $"{net:N0} ₽";
        NetText.Foreground = new SolidColorBrush(net >= 0
            ? Color.FromRgb(0x51, 0xCF, 0x66)
            : Color.FromRgb(0xFF, 0x6B, 0x6B));

        // Monthly bar chart
        var monthlyStats = await txService.GetMonthlyStatsAsync(6);
        var maxExpense = monthlyStats.Max(m => m.Expense);

        var barItems = monthlyStats.Select(m => new BarItem
        {
            Label = new DateTime(now.Year, m.Month, 1).ToString("MMM", new CultureInfo("ru-RU")),
            BarHeight = maxExpense > 0 ? Math.Max(4, (double)(m.Expense / maxExpense * 160)) : 4,
            AmountLabel = m.Expense > 0 ? $"{m.Expense / 1000:N0}k" : "0"
        }).ToList();
        BarChart.ItemsSource = barItems;

        // Category breakdown
        var categories = await txService.GetSpendingByCategoryAsync(now.Year, now.Month);
        decimal maxCat = categories.Any() ? categories.Values.Max() : 1;

        var catItems = categories
            .OrderByDescending(c => c.Value)
            .Select(c => new CategoryItem
            {
                Icon = GetCategoryIcon(c.Key),
                Category = GetCategoryName(c.Key),
                Amount = c.Value,
                BarWidth = maxCat > 0 ? Math.Max(4, (double)(c.Value / maxCat * 240)) : 4
            }).ToList();

        CategoryList.ItemsSource = catItems;
        NoCategoryText.Visibility = catItems.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        // Stats
        var expTxs = thisMonth.Where(t =>
            t.FromAccountId.HasValue && userAccountIds.Contains(t.FromAccountId.Value)).ToList();
        TxCountText.Text = txs.Count.ToString();
        AvgExpenseText.Text = expTxs.Any() ? $"{expTxs.Average(t => (double)t.Amount):N0} ₽" : "—";
        MaxExpenseText.Text = expTxs.Any() ? $"{expTxs.Max(t => t.Amount):N0} ₽" : "—";
    }

    private static string GetCategoryIcon(TransactionCategory cat) => cat switch
    {
        TransactionCategory.Food => "🍔",
        TransactionCategory.Transport => "🚗",
        TransactionCategory.Shopping => "🛍",
        TransactionCategory.Entertainment => "🎮",
        TransactionCategory.Health => "💊",
        TransactionCategory.Housing => "🏠",
        TransactionCategory.Utilities => "⚡",
        TransactionCategory.Transfer => "↔",
        TransactionCategory.Salary => "💰",
        _ => "•"
    };

    private static string GetCategoryName(TransactionCategory cat) => cat switch
    {
        TransactionCategory.Food => "Еда и рестораны",
        TransactionCategory.Transport => "Транспорт",
        TransactionCategory.Shopping => "Покупки",
        TransactionCategory.Entertainment => "Развлечения",
        TransactionCategory.Health => "Здоровье",
        TransactionCategory.Housing => "Жильё",
        TransactionCategory.Utilities => "Коммунальные",
        TransactionCategory.Transfer => "Переводы",
        TransactionCategory.Salary => "Зарплата",
        _ => "Прочее"
    };
}

public class BarItem
{
    public string Label { get; set; } = "";
    public double BarHeight { get; set; }
    public string AmountLabel { get; set; } = "";
}

public class CategoryItem
{
    public string Icon { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Amount { get; set; }
    public double BarWidth { get; set; }
}
