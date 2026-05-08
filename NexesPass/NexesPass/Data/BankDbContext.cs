using Microsoft.EntityFrameworkCore;
using NexesPass.Models;

namespace NexesPass.Data;

public class BankDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<BankCard> Cards => Set<BankCard>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NexesPass", "nexespass.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Account>()
            .HasMany(a => a.OutgoingTransactions)
            .WithOne(t => t.FromAccount)
            .HasForeignKey(t => t.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Account>()
            .HasMany(a => a.IncomingTransactions)
            .WithOne(t => t.ToAccount)
            .HasForeignKey(t => t.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        model.Entity<Account>()
            .Property(a => a.Balance)
            .HasPrecision(18, 4);

        model.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 4);

        model.Entity<Loan>()
            .Property(l => l.PrincipalAmount)
            .HasPrecision(18, 4);
    }
}
