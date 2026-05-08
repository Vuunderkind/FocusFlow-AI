using Microsoft.EntityFrameworkCore;
using NexesPass.Data;
using NexesPass.Models;

namespace NexesPass.Services;

public class AccountService
{
    private readonly BankDbContext _db;
    private readonly AuthService _auth;

    public AccountService(BankDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<List<Account>> GetUserAccountsAsync()
    {
        return await _db.Accounts
            .Where(a => a.UserId == _auth.CurrentUser!.Id && a.IsActive)
            .Include(a => a.Cards)
            .OrderByDescending(a => a.IsPrimary)
            .ToListAsync();
    }

    public async Task<Account?> GetAccountByIdAsync(int id)
    {
        return await _db.Accounts
            .Include(a => a.Cards)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == _auth.CurrentUser!.Id);
    }

    public async Task<(bool Success, string Error, Account? Account)> CreateAccountAsync(
        string name, AccountType type, Currency currency)
    {
        var account = new Account
        {
            UserId = _auth.CurrentUser!.Id,
            AccountNumber = GenerateAccountNumber(currency),
            Name = name,
            Type = type,
            Currency = currency,
            Balance = 0,
            IsPrimary = false,
            InterestRate = type == AccountType.Savings ? 8.5m : 0
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        AddNotification($"Счёт открыт",
            $"Счёт «{name}» в {currency} успешно открыт.", NotificationType.System);

        return (true, string.Empty, account);
    }

    public async Task<decimal> GetTotalBalanceInRubAsync()
    {
        var accounts = await GetUserAccountsAsync();
        var rates = await _db.ExchangeRates.ToListAsync();
        decimal total = 0;

        foreach (var acc in accounts)
        {
            if (acc.Currency == Currency.RUB)
                total += acc.Balance;
            else
            {
                var rate = rates.FirstOrDefault(r =>
                    r.FromCurrency == acc.Currency && r.ToCurrency == Currency.RUB);
                total += rate != null ? acc.Balance * rate.Rate : acc.Balance;
            }
        }
        return total;
    }

    public async Task TopUpAccountAsync(int accountId, decimal amount)
    {
        var account = await _db.Accounts.FindAsync(accountId);
        if (account == null || account.UserId != _auth.CurrentUser!.Id) return;

        account.Balance += amount;

        _db.Transactions.Add(new Transaction
        {
            ToAccountId = accountId,
            Amount = amount,
            Currency = account.Currency,
            Type = TransactionType.TopUp,
            Status = TransactionStatus.Completed,
            Description = "Пополнение счёта",
            CompletedAt = DateTime.UtcNow
        });

        AddNotification("Пополнение счёта",
            $"Счёт пополнен на {amount:N2} {account.Currency}", NotificationType.Transaction);

        await _db.SaveChangesAsync();
    }

    private void AddNotification(string title, string message, NotificationType type)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = _auth.CurrentUser!.Id,
            Type = type,
            Title = title,
            Message = message
        });
    }

    private static string GenerateAccountNumber(Currency currency)
    {
        var rng = new Random();
        string prefix = currency switch
        {
            Currency.RUB => "408",
            Currency.USD => "408",
            Currency.EUR => "408",
            _ => "408"
        };
        return $"{prefix}{rng.Next(100000000, 999999999):D9}{rng.Next(1000, 9999):D4}";
    }
}
