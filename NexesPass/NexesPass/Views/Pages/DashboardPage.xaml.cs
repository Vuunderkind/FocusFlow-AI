using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexesPass.Models;
using NexesPass.ViewModels;

namespace NexesPass.Views.Pages;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _vm;

    public DashboardPage()
    {
        InitializeComponent();
        _vm = new DashboardViewModel(App.AuthService, App.AccountService, App.TransactionService);
        Loaded += async (_, _) => await LoadDataAsync();

        // Register converters as resources
        Resources["TxTypeIcon"] = new Converters.TransactionTypeIconConverter();
        Resources["TxStatusConv"] = new TransactionStatusConverter();
    }

    private async Task LoadDataAsync()
    {
        await _vm.LoadCommand.ExecuteAsync(null);

        GreetingText.Text = _vm.Greeting + ",";
        UserNameLabel.Text = _vm.UserName;
        TotalBalanceText.Text = $"{_vm.TotalBalance:N2} ₽";

        // Build account cards
        AccountsPanel.Children.Clear();
        foreach (var acc in _vm.Accounts)
        {
            var card = BuildAccountCard(acc);
            AccountsPanel.Children.Add(card);
        }

        // Add "new account" button
        var addBtn = new Button
        {
            Style = (Style)FindResource("GhostButton"),
            Width = 180,
            Height = 110,
            Margin = new Thickness(8, 0, 0, 0)
        };
        var addContent = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
        addContent.Children.Add(new TextBlock
        {
            Text = "+",
            FontSize = 28,
            FontWeight = FontWeights.Light,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = (Brush)FindResource("TextSecondaryBrush")
        });
        addContent.Children.Add(new TextBlock
        {
            Text = "Новый счёт",
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            FontFamily = new FontFamily("Segoe UI")
        });
        addBtn.Content = addContent;
        addBtn.Click += (_, _) => NavigateToAccounts();
        AccountsPanel.Children.Add(addBtn);

        // Transactions
        var txs = _vm.RecentTransactions;
        if (txs.Count == 0)
        {
            TxList.Visibility = Visibility.Collapsed;
            EmptyTxHint.Visibility = Visibility.Visible;
        }
        else
        {
            TxList.Visibility = Visibility.Visible;
            EmptyTxHint.Visibility = Visibility.Collapsed;
            TxList.ItemsSource = txs;
        }
    }

    private Border BuildAccountCard(Account acc)
    {
        var border = new Border
        {
            Width = 180,
            Height = 110,
            CornerRadius = new CornerRadius(14),
            Margin = new Thickness(0, 0, 10, 0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var grad = new LinearGradientBrush
        {
            StartPoint = new System.Windows.Point(0, 0),
            EndPoint = new System.Windows.Point(1, 1)
        };

        (Color c1, Color c2) = acc.Currency switch
        {
            Currency.USD => (Color.FromRgb(0x16, 0xA3, 0x4A), Color.FromRgb(0x0F, 0x76, 0x33)),
            Currency.EUR => (Color.FromRgb(0x25, 0x63, 0xEB), Color.FromRgb(0x1D, 0x4E, 0xD8)),
            Currency.GBP => (Color.FromRgb(0x71, 0x71, 0x7A), Color.FromRgb(0x52, 0x52, 0x5B)),
            _ => (Color.FromRgb(0x6C, 0x63, 0xFF), Color.FromRgb(0x4E, 0xCD, 0xC4))
        };

        grad.GradientStops.Add(new GradientStop(c1, 0));
        grad.GradientStops.Add(new GradientStop(c2, 1));
        border.Background = grad;

        var panel = new StackPanel { Margin = new Thickness(16, 14, 16, 14) };
        panel.Children.Add(new TextBlock
        {
            Text = acc.Name,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
            FontFamily = new FontFamily("Segoe UI")
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{acc.Balance:N2}",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White,
            FontFamily = new FontFamily("Segoe UI"),
            Margin = new Thickness(0, 4, 0, 2)
        });
        panel.Children.Add(new TextBlock
        {
            Text = acc.Currency.ToString(),
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0xFF, 0xFF)),
            FontFamily = new FontFamily("Segoe UI")
        });

        border.Child = panel;
        return border;
    }

    private void NavigateToAccounts()
    {
        if (Window.GetWindow(this) is MainWindow mw)
            mw.GetType().GetMethod("Navigate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(mw, ["Accounts"]);
    }

    private void QuickTransfer(object sender, RoutedEventArgs e) => Navigate("Transfer");
    private void QuickTopUp(object sender, RoutedEventArgs e) => Navigate("Accounts");
    private void QuickCards(object sender, RoutedEventArgs e) => Navigate("Cards");
    private void QuickLoans(object sender, RoutedEventArgs e) => Navigate("Loans");

    private void Navigate(string page)
    {
        if (Window.GetWindow(this) is MainWindow mw)
            mw.GetType().GetMethod("Navigate",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(mw, [page]);
    }
}

public class TransactionStatusConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        return value is NexesPass.Models.TransactionStatus s ? s switch
        {
            NexesPass.Models.TransactionStatus.Completed => "Выполнен",
            NexesPass.Models.TransactionStatus.Pending => "В обработке",
            NexesPass.Models.TransactionStatus.Failed => "Ошибка",
            _ => ""
        } : "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
