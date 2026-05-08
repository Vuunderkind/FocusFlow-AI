using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NexesPass.Views.Pages;

namespace NexesPass.Views;

public partial class MainWindow : Window
{
    private Button? _activeNav;

    public MainWindow()
    {
        InitializeComponent();
        var user = App.AuthService.CurrentUser!;
        UserNameText.Text = user.FullName;
        UserInitialsText.Text = user.AvatarInitials;
        Navigate("Dashboard");
    }

    private void NavClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
            Navigate(btn.Tag?.ToString() ?? "Dashboard");
    }

    private void Navigate(string page)
    {
        // Update nav styles
        if (_activeNav != null)
            _activeNav.Style = (Style)FindResource("NavButton");

        Page? target = page switch
        {
            "Dashboard" => new DashboardPage(),
            "Accounts" => new AccountsPage(),
            "Cards" => new CardsPage(),
            "Transfer" => new TransferPage(),
            "Loans" => new LoansPage(),
            "Analytics" => new AnalyticsPage(),
            _ => new DashboardPage()
        };

        PageTitle.Text = page switch
        {
            "Dashboard" => "Главная",
            "Accounts" => "Счета",
            "Cards" => "Карты",
            "Transfer" => "Переводы",
            "Loans" => "Кредиты",
            "Analytics" => "Аналитика",
            _ => "Главная"
        };

        MainFrame.Navigate(target);

        _activeNav = page switch
        {
            "Dashboard" => BtnDashboard,
            "Accounts" => BtnAccounts,
            "Cards" => BtnCards,
            "Transfer" => BtnTransfer,
            "Loans" => BtnLoans,
            "Analytics" => BtnAnalytics,
            _ => BtnDashboard
        };
        if (_activeNav != null)
            _activeNav.Style = (Style)FindResource("ActiveNavButton");
    }

    private void LogoutClick(object sender, RoutedEventArgs e)
    {
        App.AuthService.Logout();
        new LoginWindow().Show();
        Close();
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void DragWindow(object sender, MouseButtonEventArgs e) => DragMove();
}
