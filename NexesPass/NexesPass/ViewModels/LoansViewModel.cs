using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexesPass.Models;
using NexesPass.Services;

namespace NexesPass.ViewModels;

public partial class LoansViewModel : BaseViewModel
{
    private readonly LoanService _loanService;
    private readonly AccountService _accountService;

    [ObservableProperty] private List<Loan> _loans = new();
    [ObservableProperty] private List<Account> _accounts = new();
    [ObservableProperty] private Loan? _selectedLoan;
    [ObservableProperty] private bool _showApplyForm;
    [ObservableProperty] private Account? _selectedAccount;
    [ObservableProperty] private string _selectedLoanType = "Personal";
    [ObservableProperty] private decimal _loanAmount = 100000;
    [ObservableProperty] private int _termMonths = 24;
    [ObservableProperty] private string _purpose = string.Empty;
    [ObservableProperty] private decimal _calculatedMonthly;
    [ObservableProperty] private decimal _calculatedTotal;

    public LoansViewModel(LoanService loanService, AccountService accountService)
    {
        _loanService = loanService;
        _accountService = accountService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        Loans = await _loanService.GetUserLoansAsync();
        Accounts = await _accountService.GetUserAccountsAsync();
        SelectedLoan = Loans.FirstOrDefault();
        Recalculate();
    }

    [RelayCommand]
    public void RecalculateCommand()
    {
        Recalculate();
    }

    private void Recalculate()
    {
        var type = Enum.Parse<LoanType>(SelectedLoanType);
        decimal rate = type switch
        {
            LoanType.Personal => 14.9m,
            LoanType.Mortgage => 8.5m,
            LoanType.Auto => 11.5m,
            LoanType.Business => 16.5m,
            _ => 14.9m
        };
        CalculatedMonthly = LoanService.CalculateMonthlyPayment(LoanAmount, rate, TermMonths);
        CalculatedTotal = CalculatedMonthly * TermMonths;
    }

    [RelayCommand]
    public async Task ApplyAsync()
    {
        if (SelectedAccount == null)
        {
            SetError("Выберите счёт для зачисления");
            return;
        }

        IsBusy = true;
        try
        {
            var type = Enum.Parse<LoanType>(SelectedLoanType);
            var (ok, err, _) = await _loanService.ApplyForLoanAsync(
                SelectedAccount.Id, type, LoanAmount, TermMonths, Purpose);

            if (ok)
            {
                SetSuccess("Кредит одобрен и зачислен на счёт!");
                ShowApplyForm = false;
                await LoadAsync();
            }
            else SetError(err);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task MakePaymentAsync(int loanId)
    {
        IsBusy = true;
        try
        {
            bool ok = await _loanService.MakePaymentAsync(loanId);
            if (ok)
            {
                SetSuccess("Платёж выполнен успешно!");
                await LoadAsync();
            }
            else
                SetError("Недостаточно средств или кредит уже погашен");
        }
        finally { IsBusy = false; }
    }
}
