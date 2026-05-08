using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexesPass.Models;
using NexesPass.Services;

namespace NexesPass.ViewModels;

public partial class AccountsViewModel : BaseViewModel
{
    private readonly AccountService _accountService;
    private readonly TransactionService _txService;

    [ObservableProperty] private List<Account> _accounts = new();
    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private List<Transaction> _transactions = new();
    [ObservableProperty] private bool _showCreateForm;
    [ObservableProperty] private string _newAccountName = string.Empty;
    [ObservableProperty] private string _selectedType = "Checking";
    [ObservableProperty] private string _selectedCurrency = "RUB";
    [ObservableProperty] private decimal _topUpAmount;
    [ObservableProperty] private bool _showTopUp;

    public AccountsViewModel(AccountService accountService, TransactionService txService)
    {
        _accountService = accountService;
        _txService = txService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        Accounts = await _accountService.GetUserAccountsAsync();
        SelectedAccount = Accounts.FirstOrDefault();
        if (SelectedAccount != null)
            await LoadTransactionsAsync();
    }

    [RelayCommand]
    public async Task SelectAccountAsync(Account account)
    {
        SelectedAccount = account;
        await LoadTransactionsAsync();
    }

    private async Task LoadTransactionsAsync()
    {
        if (SelectedAccount == null) return;
        Transactions = await _txService.GetRecentTransactionsAsync(SelectedAccount.Id, 30);
    }

    [RelayCommand]
    public async Task CreateAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountName))
        {
            SetError("Введите название счёта");
            return;
        }

        IsBusy = true;
        try
        {
            var type = Enum.Parse<AccountType>(SelectedType);
            var currency = Enum.Parse<Currency>(SelectedCurrency);
            var (ok, err, acc) = await _accountService.CreateAccountAsync(NewAccountName, type, currency);
            if (ok)
            {
                SetSuccess("Счёт успешно открыт!");
                ShowCreateForm = false;
                NewAccountName = string.Empty;
                await LoadAsync();
            }
            else SetError(err);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    public async Task TopUpAsync()
    {
        if (SelectedAccount == null || TopUpAmount <= 0)
        {
            SetError("Введите корректную сумму пополнения");
            return;
        }
        IsBusy = true;
        try
        {
            await _accountService.TopUpAccountAsync(SelectedAccount.Id, TopUpAmount);
            SetSuccess($"Счёт пополнен на {TopUpAmount:N2}!");
            ShowTopUp = false;
            TopUpAmount = 0;
            await LoadAsync();
        }
        finally { IsBusy = false; }
    }
}
