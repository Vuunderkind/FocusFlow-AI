using Microsoft.EntityFrameworkCore;
using NexesPass.Data;
using NexesPass.Models;

namespace NexesPass.Services;

public class AuthService
{
    private readonly BankDbContext _db;
    public User? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public AuthService(BankDbContext db) => _db = db;

    public async Task<(bool Success, string Error)> RegisterAsync(
        string firstName, string lastName, string email, string phone, string password, string pin)
    {
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return (false, "Пользователь с таким email уже существует");

        if (await _db.Users.AnyAsync(u => u.Phone == phone))
            return (false, "Пользователь с таким номером телефона уже существует");

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email.ToLower().Trim(),
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            PinHash = BCrypt.Net.BCrypt.HashPassword(pin),
            CreatedAt = DateTime.UtcNow,
            IsVerified = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Create default RUB account
        var account = new Account
        {
            UserId = user.Id,
            AccountNumber = GenerateAccountNumber(),
            Name = "Основной счёт",
            Type = AccountType.Checking,
            Currency = Currency.RUB,
            Balance = 0,
            IsPrimary = true
        };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        // Issue default card
        var card = CreateCard(account.Id, user.FullName, CardType.Debit, CardNetwork.Mir, "#6C63FF");
        _db.Cards.Add(card);

        // Welcome notification
        _db.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Type = NotificationType.System,
            Title = "Добро пожаловать в Nexes Pass!",
            Message = $"Привет, {firstName}! Ваш счёт успешно открыт. Пополните его и начните пользоваться.",
            Icon = "Star"
        });

        await _db.SaveChangesAsync();
        CurrentUser = user;
        return (true, string.Empty);
    }

    public async Task<(bool Success, string Error)> LoginAsync(string email, string password)
    {
        var user = await _db.Users
            .Include(u => u.Accounts)
            .Include(u => u.Notifications)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return (false, "Неверный email или пароль");

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        CurrentUser = user;
        return (true, string.Empty);
    }

    public void Logout() => CurrentUser = null;

    private static string GenerateAccountNumber()
    {
        var rng = new Random();
        return $"408{rng.Next(100000000, 999999999):D9}{rng.Next(1000, 9999):D4}";
    }

    private static BankCard CreateCard(int accountId, string holderName, CardType type, CardNetwork network, string color)
    {
        var rng = new Random();
        string prefix = network switch
        {
            CardNetwork.Visa => "4",
            CardNetwork.Mastercard => "5",
            CardNetwork.Mir => "2",
            _ => "4"
        };

        var number = GenerateCardNumber(prefix);
        var expYear = (DateTime.Now.Year + 4).ToString()[^2..];

        return new BankCard
        {
            AccountId = accountId,
            CardNumber = number,
            CardholderName = holderName.ToUpper(),
            ExpiryMonth = "12",
            ExpiryYear = expYear,
            CvvHash = BCrypt.Net.BCrypt.HashPassword(rng.Next(100, 999).ToString()),
            Type = type,
            Network = network,
            Status = CardStatus.Active,
            Color = color,
            DailyLimit = 100000,
            MonthlyLimit = 500000
        };
    }

    private static string GenerateCardNumber(string prefix)
    {
        var rng = new Random();
        var digits = prefix.Select(c => int.Parse(c.ToString())).ToList();
        while (digits.Count < 15)
            digits.Add(rng.Next(0, 10));

        // Luhn algorithm check digit
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
