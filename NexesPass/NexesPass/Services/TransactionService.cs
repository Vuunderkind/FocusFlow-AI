using Microsoft.EntityFrameworkCore;
using NexesPass.Data;
using NexesPass.Models;

namespace NexesPass.Services;

public class TransactionService
{
    private readonly BankDbContext _db;
    private readonly AuthService _auth;

    public TransactionService(BankDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<List<Transaction>> GetRecentTransactionsAsync(int? accountId = null, int count = 50)
    {
        var userAccountIds = await _db.Accounts
            .Where(a => a.UserId == _auth.CurrentUser!.Id)
            .Select(a => a.Id)
            .ToListAsync();

        var query = _db.Transactions
            .Include(t => t.FromAccount)
            .Include(t => t.ToAccount)
            .Where(t =>
                (t.FromAccountId.HasValue && userAccountIds.Contains(t.FromAccountId.Value)) ||
                (t.ToAccountId.HasValue && userAccountIds.Contains(t.ToAccountId.Value)));

        if (accountId.HasValue)
            query = query.Where(t => t.FromAccountId == accountId || t.ToAccountId == accountId);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<(bool Success, string Error)> TransferAsync(
        int fromAccountId, int toAccountId, decimal amount, string description)
    {
        var fromAcc = await _db.Accounts.FindAsync(fromAccountId);
        var toAcc = await _db.Accounts.FindAsync(toAccountId);

        if (fromAcc == null || fromAcc.UserId != _auth.CurrentUser!.Id)
            return (false, "Счёт отправителя не найден");
        if (toAcc == null)
            return (false, "Счёт получателя не найден");
        if (fromAcc.AvailableBalance < amount)
            return (false, "Недостаточно средств на счёте");
        if (amount <= 0)
            return (false, "Сумма должна быть больше нуля");

        decimal fee = 0;
        if (fromAcc.UserId != toAcc.UserId)
            fee = Math.Round(amount * 0.005m, 2); // 0.5% for external

        fromAcc.Balance -= (amount + fee);
        toAcc.Balance += amount;

        var tx = new Transaction
        {
            FromAccountId = fromAccountId,
            ToAccountId = toAccountId,
            Amount = amount,
            Fee = fee,
            Currency = fromAcc.Currency,
            Type = TransactionType.Transfer,
            Status = TransactionStatus.Completed,
            Description = string.IsNullOrWhiteSpace(description) ? "Перевод" : description,
            CompletedAt = DateTime.UtcNow,
            Reference = GenerateReference()
        };
        _db.Transactions.Add(tx);

        _db.Notifications.Add(new Notification
        {
            UserId = _auth.CurrentUser.Id,
            Type = NotificationType.Transaction,
            Title = "Перевод выполнен",
            Message = $"Переведено {amount:N2} {fromAcc.Currency} на счёт {toAcc.AccountNumber[^4..]}"
        });

        await _db.SaveChangesAsync();
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> PayByPhoneAsync(
        int fromAccountId, string phone, decimal amount, string description)
    {
        var recipient = await _db.Users.FirstOrDefaultAsync(u => u.Phone == phone);
        if (recipient == null)
            return (false, "Пользователь с таким номером не найден в системе Nexes Pass");

        var toAcc = await _db.Accounts
            .Where(a => a.UserId == recipient.Id && a.IsPrimary)
            .FirstOrDefaultAsync();

        if (toAcc == null)
            return (false, "У получателя нет активных счетов");

        return await TransferAsync(fromAccountId, toAcc.Id, amount,
            description.IsNullOrEmpty() ? $"Перевод по номеру {phone}" : description);
    }

    public async Task<Dictionary<TransactionCategory, decimal>> GetSpendingByCategoryAsync(
        int year, int month)
    {
        var userAccountIds = await _db.Accounts
            .Where(a => a.UserId == _auth.CurrentUser!.Id)
            .Select(a => a.Id)
            .ToListAsync();

        var txs = await _db.Transactions
            .Where(t =>
                t.FromAccountId.HasValue &&
                userAccountIds.Contains(t.FromAccountId.Value) &&
                t.CreatedAt.Year == year &&
                t.CreatedAt.Month == month &&
                t.Status == TransactionStatus.Completed)
            .ToListAsync();

        return txs.GroupBy(t => t.Category)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
    }

    public async Task<List<(int Month, decimal Income, decimal Expense)>> GetMonthlyStatsAsync(int months = 6)
    {
        var userAccountIds = await _db.Accounts
            .Where(a => a.UserId == _auth.CurrentUser!.Id)
            .Select(a => a.Id)
            .ToListAsync();

        var result = new List<(int Month, decimal Income, decimal Expense)>();
        var now = DateTime.UtcNow;

        for (int i = months - 1; i >= 0; i--)
        {
            var target = now.AddMonths(-i);
            var txs = await _db.Transactions
                .Where(t =>
                    t.CreatedAt.Year == target.Year &&
                    t.CreatedAt.Month == target.Month &&
                    t.Status == TransactionStatus.Completed &&
                    ((t.FromAccountId.HasValue && userAccountIds.Contains(t.FromAccountId.Value)) ||
                     (t.ToAccountId.HasValue && userAccountIds.Contains(t.ToAccountId.Value))))
                .ToListAsync();

            decimal income = txs
                .Where(t => t.ToAccountId.HasValue && userAccountIds.Contains(t.ToAccountId.Value)
                    && t.Type != TransactionType.Transfer)
                .Sum(t => t.Amount);

            decimal expense = txs
                .Where(t => t.FromAccountId.HasValue && userAccountIds.Contains(t.FromAccountId.Value))
                .Sum(t => t.Amount);

            result.Add((target.Month, income, expense));
        }
        return result;
    }

    private static string GenerateReference()
    {
        return $"NXP{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
    }
}

public static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? s) => string.IsNullOrEmpty(s);
}
