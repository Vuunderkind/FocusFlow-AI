namespace NexesPass.Models;

public enum TransactionType
{
    Transfer, Payment, TopUp, Withdrawal,
    CardPayment, CashBack, Interest, LoanPayment, Fee
}

public enum TransactionStatus { Pending, Completed, Failed, Cancelled, Reversed }

public enum TransactionCategory
{
    Transfer, Food, Transport, Shopping, Entertainment,
    Health, Housing, Utilities, Salary, Other
}

public class Transaction
{
    public int Id { get; set; }
    public string TransactionId { get; set; } = Guid.NewGuid().ToString("N")[..16].ToUpper();
    public int? FromAccountId { get; set; }
    public Account? FromAccount { get; set; }
    public int? ToAccountId { get; set; }
    public Account? ToAccount { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }
    public decimal Fee { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;
    public TransactionCategory Category { get; set; } = TransactionCategory.Transfer;
    public string Description { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientDetails { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public string Reference { get; set; } = string.Empty;
}
