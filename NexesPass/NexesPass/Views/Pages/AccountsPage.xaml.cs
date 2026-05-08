using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NexesPass.Converters;
using NexesPass.Models;
using NexesPass.ViewModels;

namespace NexesPass.Views.Pages;

public partial class AccountsPage : Page
{
    private readonly AccountsViewModel _vm;
    private Account? _selectedAccount;

    public AccountsPage()
    {
        InitializeComponent();
        Resources["AccTypeConv"] = new AccountTypeConverter();
        Resources["TxIconConv"] = new TransactionTypeIconConverter();
        _vm = new AccountsViewModel(App.AccountService, App.TransactionService);
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _vm.LoadCommand.ExecuteAsync(null);
        AccountList.ItemsSource = _vm.Accounts;
        if (_vm.SelectedAccount != null)
            SelectAccountDisplay(_vm.SelectedAccount);
    }

    private void SelectAccount(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is Account acc)
            SelectAccountDisplay(acc);
    }

    private void SelectAccountDisplay(Account acc)
    {
        _selectedAccount = acc;
        AccNameText.Text = acc.Name;
        AccNumberText.Text = acc.AccountNumber;
        AccBalanceText.Text = $"{acc.Balance:N2} {acc.Currency}";
        AccTypeText.Text = acc.Type switch
        {
            AccountType.Checking => "Текущий",
            AccountType.Savings => "Накопительный",
            AccountType.Investment => "Инвестиционный",
            _ => ""
        };
        AccCurrencyText.Text = acc.Currency.ToString();
        AccDateText.Text = acc.CreatedAt.ToString("dd.MM.yyyy");

        var txs = App.TransactionService
            .GetRecentTransactionsAsync(acc.Id, 20).GetAwaiter().GetResult();

        TxList.ItemsSource = txs;
        NoTxText.Visibility = txs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowCreateForm(object sender, RoutedEventArgs e) =>
        CreateForm.Visibility = Visibility.Visible;
    private void HideCreateForm(object sender, RoutedEventArgs e) =>
        CreateForm.Visibility = Visibility.Collapsed;

    private async void ExecuteCreate(object sender, RoutedEventArgs e)
    {
        _vm.NewAccountName = AccNameBox.Text;
        _vm.SelectedType = AccTypeBox.SelectedIndex switch { 1 => "Savings", 2 => "Investment", _ => "Checking" };
        _vm.SelectedCurrency = AccCurrencyBox.SelectedIndex switch { 1 => "USD", 2 => "EUR", 3 => "GBP", _ => "RUB" };

        await _vm.CreateAccountCommand.ExecuteAsync(null);

        if (!string.IsNullOrEmpty(_vm.ErrorMessage))
        {
            CreateErrText.Text = _vm.ErrorMessage;
            CreateErrBorder.Visibility = Visibility.Visible;
        }
        else
        {
            CreateForm.Visibility = Visibility.Collapsed;
            CreateErrBorder.Visibility = Visibility.Collapsed;
            await LoadAsync();
        }
    }

    private void ShowTopUp(object sender, RoutedEventArgs e) =>
        TopUpForm.Visibility = Visibility.Visible;
    private void HideTopUp(object sender, RoutedEventArgs e) =>
        TopUpForm.Visibility = Visibility.Collapsed;

    private async void ExecuteTopUp(object sender, RoutedEventArgs e)
    {
        if (_selectedAccount == null) return;
        if (!decimal.TryParse(TopUpAmountBox.Text, out decimal amount)) return;

        _vm.SelectedAccount = _selectedAccount;
        _vm.TopUpAmount = amount;
        await _vm.TopUpCommand.ExecuteAsync(null);

        TopUpForm.Visibility = Visibility.Collapsed;
        await LoadAsync();
        SelectAccountDisplay(_vm.Accounts.First(a => a.Id == _selectedAccount.Id));
    }
}
