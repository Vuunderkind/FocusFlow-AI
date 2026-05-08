namespace NexesPass.Models;

public enum LoanStatus { Active, PaidOff, Overdue, Rejected, Pending }
public enum LoanType { Personal, Mortgage, Auto, Business }

public class Loan
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public LoanType Type { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Active;
    public decimal PrincipalAmount { get; set; }
    public decimal RemainingBalance { get; set; }
    public decimal InterestRate { get; set; }
    public decimal MonthlyPayment { get; set; }
    public int TermMonths { get; set; }
    public int RemainingMonths { get; set; }
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime NextPaymentDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public decimal TotalPaid { get; set; }
    public decimal TotalInterestPaid { get; set; }

    public decimal TotalCost => MonthlyPayment * TermMonths;
    public decimal ProgressPercent => PrincipalAmount > 0
        ? (decimal)((PrincipalAmount - RemainingBalance) / PrincipalAmount * 100)
        : 0;
}
