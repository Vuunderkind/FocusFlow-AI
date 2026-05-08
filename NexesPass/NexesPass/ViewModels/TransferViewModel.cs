using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexesPass.Models;
using NexesPass.Services;

namespace NexesPass.ViewModels;

public partial class TransferViewModel : BaseViewModel
{
    private readonly TransactionService _txService;
    private readonly AccountService _accountService;

    [ObservableProperty] private List<Account> _accounts = new();
    [ObservableProperty] private Account? _selectedFromAccount;
    [ObservableProperty] private string _toAccountNumber = string.Empty;
    [ObservableProperty] private string _recipientPhone = string.Empty;
    [ObservableProperty] private decimal _amount;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _usePhone;
    [ObservableProperty] private bool _transferSuccess;

    public TransferViewModel(TransactionService txService, AccountService accountService)
    {
        _txService = txService;
        _accountService = accountService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        Accounts = await _accountService.GetUserAccountsAsync();
        SelectedFromAccount = Accounts.FirstOrDefault(a => a.IsPrimary) ?? Accounts.FirstOrDefault();
    }

    [RelayCommand]
    public async Task ExecuteTransferAsync()
    {
        ClearMessages();
        if (SelectedFromAccount == null)
        {
            SetError("Выберите счёт списания");
            return;
        }
        if (Amount <= 0)
        {
            SetError("Введите корректную сумму");
            return;
        }

        IsBusy = true;
        try
        {
            if (UsePhone)
            {
                if (string.IsNullOrWhiteSpace(RecipientPhone))
                {
                    SetError("Введите номер телефона получателя");
                    return;
                }
                var (ok, err) = await _txService.PayByPhoneAsync(
                    SelectedFromAccount.Id, RecipientPhone, Amount, Description);
                if (ok)
                {
                    TransferSuccess = true;
                    SetSuccess($"Перевод {Amount:N2} ₽ выполнен успешно!");
                    Amount = 0;
                    RecipientPhone = string.Empty;
                }
                else SetError(err);
            }
            else
            {
                // Find account by number
                var allAccounts = await _accountService.GetUserAccountsAsync();
                // For demo: transfer to another own account
                var toAcc = allAccounts.FirstOrDefault(a =>
                    a.AccountNumber == ToAccountNumber && a.Id != SelectedFromAccount.Id);

                if (toAcc == null)
                {
                    SetError("Счёт получателя не найден. Для демо используйте номер своего второго счёта.");
                    return;
                }

                var (ok, err) = await _txService.TransferAsync(
                    SelectedFromAccount.Id, toAcc.Id, Amount, Description);
                if (ok)
                {
                    TransferSuccess = true;
                    SetSuccess($"Перевод {Amount:N2} ₽ выполнен успешно!");
                    Amount = 0;
                    ToAccountNumber = string.Empty;
                }
                else SetError(err);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
