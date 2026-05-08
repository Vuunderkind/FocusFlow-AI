using System.Windows;
using NexesPass.Data;
using NexesPass.Services;

namespace NexesPass;

public partial class App : Application
{
    public static BankDbContext Database { get; private set; } = null!;
    public static AuthService AuthService { get; private set; } = null!;
    public static AccountService AccountService { get; private set; } = null!;
    public static TransactionService TransactionService { get; private set; } = null!;
    public static CardService CardService { get; private set; } = null!;
    public static LoanService LoanService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Database = new BankDbContext();
        DatabaseInitializer.Initialize(Database);

        AuthService = new AuthService(Database);
        AccountService = new AccountService(Database, AuthService);
        TransactionService = new TransactionService(Database, AuthService);
        CardService = new CardService(Database, AuthService);
        LoanService = new LoanService(Database, AuthService);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Database?.Dispose();
        base.OnExit(e);
    }
}
