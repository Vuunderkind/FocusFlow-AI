namespace NexesPass.Models;

public enum CardType { Debit, Credit, Virtual }
public enum CardNetwork { Visa, Mastercard, Mir }
public enum CardStatus { Active, Frozen, Blocked, Expired }

public class BankCard
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string CardNumber { get; set; } = string.Empty;
    public string MaskedNumber => $"**** **** **** {CardNumber[^4..]}";
    public string CardholderName { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string Expiry => $"{ExpiryMonth}/{ExpiryYear}";
    public string CvvHash { get; set; } = string.Empty;
    public CardType Type { get; set; }
    public CardNetwork Network { get; set; }
    public CardStatus Status { get; set; } = CardStatus.Active;
    public decimal DailyLimit { get; set; } = 100000;
    public decimal MonthlyLimit { get; set; } = 500000;
    public decimal DailySpent { get; set; }
    public decimal MonthlySpent { get; set; }
    public bool ContactlessEnabled { get; set; } = true;
    public bool OnlinePaymentsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Color { get; set; } = "#6C63FF";
}
