using Microsoft.EntityFrameworkCore;
using NexesPass.Data;
using NexesPass.Models;

namespace NexesPass.Services;

public class LoanService
{
    private readonly BankDbContext _db;
    private readonly AuthService _auth;

    public LoanService(BankDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<List<Loan>> GetUserLoansAsync()
    {
        return await _db.Loans
            .Where(l => l.UserId == _auth.CurrentUser!.Id)
            .Include(l => l.Account)
            .OrderByDescending(l => l.StartDate)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error, Loan? Loan)> ApplyForLoanAsync(
        int accountId, LoanType type, decimal amount, int termMonths, string purpose)
    {
        var account = await _db.Accounts.FindAsync(accountId);
        if (account == null || account.UserId != _auth.CurrentUser!.Id)
            return (false, "Счёт не найден", null);

        if (amount < 10000)
            return (false, "Минимальная сумма кредита 10 000 ₽", null);

        if (amount > 10_000_000)
            return (false, "Максимальная сумма кредита 10 000 000 ₽", null);

        decimal rate = type switch
        {
            LoanType.Personal => 14.9m,
            LoanType.Mortgage => 8.5m,
            LoanType.Auto => 11.5m,
            LoanType.Business => 16.5m,
            _ => 14.9m
        };

        decimal monthlyPayment = CalculateMonthlyPayment(amount, rate, termMonths);

        var loan = new Loan
        {
            UserId = _auth.CurrentUser!.Id,
            AccountId = accountId,
            Type = type,
            Status = LoanStatus.Active,
            PrincipalAmount = amount,
            RemainingBalance = amount,
            InterestRate = rate,
            MonthlyPayment = monthlyPayment,
            TermMonths = termMonths,
            RemainingMonths = termMonths,
            StartDate = DateTime.UtcNow,
            NextPaymentDate = DateTime.UtcNow.AddMonths(1),
            EndDate = DateTime.UtcNow.AddMonths(termMonths),
            Purpose = purpose
        };

        _db.Loans.Add(loan);

        // Credit loan to account
        account.Balance += amount;
        _db.Transactions.Add(new Transaction
        {
            ToAccountId = accountId,
            Amount = amount,
            Currency = account.Currency,
            Type = TransactionType.TopUp,
            Status = TransactionStatus.Completed,
            Description = $"Выдача кредита: {GetLoanTypeName(type)}",
            CompletedAt = DateTime.UtcNow
        });

        _db.Notifications.Add(new Notification
        {
            UserId = _auth.CurrentUser.Id,
            Type = NotificationType.Loan,
            Title = "Кредит одобрен!",
            Message = $"{GetLoanTypeName(type)} на {amount:N0} ₽ зачислен на счёт. Ежемесячный платёж: {monthlyPayment:N2} ₽"
        });

        await _db.SaveChangesAsync();
        return (true, string.Empty, loan);
    }

    public async Task<bool> MakePaymentAsync(int loanId)
    {
        var loan = await _db.Loans
            .Include(l => l.Account)
            .FirstOrDefaultAsync(l => l.Id == loanId && l.UserId == _auth.CurrentUser!.Id);

        if (loan == null || loan.Status != LoanStatus.Active) return false;
        if (loan.Account.AvailableBalance < loan.MonthlyPayment) return false;

        decimal interest = Math.Round(loan.RemainingBalance * loan.InterestRate / 100 / 12, 2);
        decimal principal = loan.MonthlyPayment - interest;

        loan.Account.Balance -= loan.MonthlyPayment;
        loan.RemainingBalance = Math.Max(0, loan.RemainingBalance - principal);
        loan.TotalPaid += loan.MonthlyPayment;
        loan.TotalInterestPaid += interest;
        loan.RemainingMonths = Math.Max(0, loan.RemainingMonths - 1);
        loan.NextPaymentDate = loan.NextPaymentDate.AddMonths(1);

        if (loan.RemainingBalance <= 0 || loan.RemainingMonths == 0)
            loan.Status = LoanStatus.PaidOff;

        _db.Transactions.Add(new Transaction
        {
            FromAccountId = loan.AccountId,
            Amount = loan.MonthlyPayment,
            Currency = loan.Account.Currency,
            Type = TransactionType.LoanPayment,
            Status = TransactionStatus.Completed,
            Description = $"Платёж по кредиту #{loan.Id}",
            CompletedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public static decimal CalculateMonthlyPayment(decimal amount, decimal annualRate, int termMonths)
    {
        if (annualRate == 0) return amount / termMonths;
        double r = (double)(annualRate / 100 / 12);
        double n = termMonths;
        double a = (double)amount;
        return (decimal)(a * r * Math.Pow(1 + r, n) / (Math.Pow(1 + r, n) - 1));
    }

    private static string GetLoanTypeName(LoanType type) => type switch
    {
        LoanType.Personal => "Потребительский кредит",
        LoanType.Mortgage => "Ипотека",
        LoanType.Auto => "Автокредит",
        LoanType.Business => "Бизнес-кредит",
        _ => "Кредит"
    };
}
