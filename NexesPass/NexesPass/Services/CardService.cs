using Microsoft.EntityFrameworkCore;
using NexesPass.Data;
using NexesPass.Models;

namespace NexesPass.Services;

public class CardService
{
    private readonly BankDbContext _db;
    private readonly AuthService _auth;

    public CardService(BankDbContext db, AuthService auth)
    {
        _db = db;
        _auth = auth;
    }

    public async Task<List<BankCard>> GetUserCardsAsync()
    {
        var userAccountIds = await _db.Accounts
            .Where(a => a.UserId == _auth.CurrentUser!.Id)
            .Select(a => a.Id)
            .ToListAsync();

        return await _db.Cards
            .Include(c => c.Account)
            .Where(c => userAccountIds.Contains(c.AccountId))
            .ToListAsync();
    }

    public async Task<(bool Success, string Error, BankCard? Card)> IssueCardAsync(
        int accountId, CardType type, CardNetwork network, string color)
    {
        var account = await _db.Accounts.FindAsync(accountId);
        if (account == null || account.UserId != _auth.CurrentUser!.Id)
            return (false, "Счёт не найден", null);

        var existingCards = await _db.Cards.CountAsync(c => c.AccountId == accountId);
        if (existingCards >= 5)
            return (false, "Максимум 5 карт на счёт", null);

        var user = _auth.CurrentUser!;
        var card = new BankCard
        {
            AccountId = accountId,
            CardNumber = GenerateCardNumber(network),
            CardholderName = user.FullName.ToUpper(),
            ExpiryMonth = "12",
            ExpiryYear = (DateTime.Now.Year + 4).ToString()[^2..],
            CvvHash = BCrypt.Net.BCrypt.HashPassword(new Random().Next(100, 999).ToString()),
            Type = type,
            Network = network,
            Status = CardStatus.Active,
            Color = color,
            DailyLimit = type == CardType.Credit ? 200000 : 100000,
            MonthlyLimit = type == CardType.Credit ? 1000000 : 500000
        };

        _db.Cards.Add(card);
        _db.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Type = NotificationType.System,
            Title = "Карта выпущена",
            Message = $"Новая {type} карта {network} *{card.CardNumber[^4..]} готова к использованию"
        });

        await _db.SaveChangesAsync();
        return (true, string.Empty, card);
    }

    public async Task<bool> FreezeCardAsync(int cardId, bool freeze)
    {
        var userAccountIds = await _db.Accounts
            .Where(a => a.UserId == _auth.CurrentUser!.Id)
            .Select(a => a.Id)
            .ToListAsync();

        var card = await _db.Cards
            .Where(c => c.Id == cardId && userAccountIds.Contains(c.AccountId))
            .FirstOrDefaultAsync();

        if (card == null) return false;

        card.Status = freeze ? CardStatus.Frozen : CardStatus.Active;

        _db.Notifications.Add(new Notification
        {
            UserId = _auth.CurrentUser!.Id,
            Type = NotificationType.Security,
            Title = freeze ? "Карта заморожена" : "Карта разморожена",
            Message = $"Карта *{card.CardNumber[^4..]} {(freeze ? "заблокирована" : "активирована")}"
        });

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateCardLimitsAsync(int cardId, decimal daily, decimal monthly)
    {
        var userAccountIds = await _db.Accounts
            .Where(a => a.UserId == _auth.CurrentUser!.Id)
            .Select(a => a.Id)
            .ToListAsync();

        var card = await _db.Cards
            .Where(c => c.Id == cardId && userAccountIds.Contains(c.AccountId))
            .FirstOrDefaultAsync();

        if (card == null) return;
        card.DailyLimit = daily;
        card.MonthlyLimit = monthly;
        await _db.SaveChangesAsync();
    }

    private static string GenerateCardNumber(CardNetwork network)
    {
        var rng = new Random();
        string prefix = network switch
        {
            CardNetwork.Visa => "4",
            CardNetwork.Mastercard => "51",
            CardNetwork.Mir => "2200",
            _ => "4"
        };

        var digits = prefix.Select(c => int.Parse(c.ToString())).ToList();
        while (digits.Count < 15)
            digits.Add(rng.Next(0, 10));

        int sum = 0;
        bool doubleIt = true;
        for (int i = digits.Count - 1; i >= 0; i--)
        {
            int d = digits[i];
            if (doubleIt) { d *= 2; if (d > 9) d -= 9; }
            sum += d;
            doubleIt = !doubleIt;
        }
        digits.Add((10 - (sum % 10)) % 10);
        return string.Join("", digits);
    }
}
