namespace NexesPass.Models;

public enum AccountType { Checking, Savings, Investment }
public enum Currency { RUB, USD, EUR, GBP, CNY }

public class Account
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string AccountNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public Currency Currency { get; set; }
    public decimal Balance { get; set; }
    public decimal ReservedBalance { get; set; }
    public decimal AvailableBalance => Balance - ReservedBalance;
    public bool IsActive { get; set; } = true;
    public bool IsPrimary { get; set; }
    public decimal InterestRate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BankCard> Cards { get; set; } = new List<BankCard>();
    public ICollection<Transaction> OutgoingTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> IncomingTransactions { get; set; } = new List<Transaction>();
}
