namespace NexesPass.Models;

public class ExchangeRate
{
    public int Id { get; set; }
    public Currency FromCurrency { get; set; }
    public Currency ToCurrency { get; set; }
    public decimal Rate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
