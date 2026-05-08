using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexesPass.Models;
using NexesPass.Services;

namespace NexesPass.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly AuthService _auth;
    private readonly AccountService _accountService;
    private readonly TransactionService _txService;

    [ObservableProperty] private decimal _totalBalance;
    [ObservableProperty] private string _greeting = string.Empty;
    [ObservableProperty] private List<Account> _accounts = new();
    [ObservableProperty] private List<Transaction> _recentTransactions = new();
    [ObservableProperty] private int _unreadNotifications;
    [ObservableProperty] private string _userName = string.Empty;
    [ObservableProperty] private string _userInitials = string.Empty;

    public DashboardViewModel(AuthService auth, AccountService accountService, TransactionService txService)
    {
        _auth = auth;
        _accountService = accountService;
        _txService = txService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var user = _auth.CurrentUser!;
            UserName = user.FullName;
            UserInitials = user.AvatarInitials;

            var hour = DateTime.Now.Hour;
            Greeting = hour < 12 ? "Доброе утро" : hour < 18 ? "Добрый день" : "Добрый вечер";

            Accounts = await _accountService.GetUserAccountsAsync();
            TotalBalance = await _accountService.GetTotalBalanceInRubAsync();
            RecentTransactions = await _txService.GetRecentTransactionsAsync(count: 10);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string FormatAmount(decimal amount, Currency currency)
    {
        string symbol = currency switch
        {
            Currency.RUB => "₽",
            Currency.USD => "$",
            Currency.EUR => "€",
            Currency.GBP => "£",
            Currency.CNY => "¥",
            _ => ""
        };
        return $"{amount:N2} {symbol}";
    }
}
