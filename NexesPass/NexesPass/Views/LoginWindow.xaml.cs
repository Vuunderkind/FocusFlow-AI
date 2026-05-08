using System.Windows;
using System.Windows.Input;
using NexesPass.ViewModels;

namespace NexesPass.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow()
    {
        InitializeComponent();
        _vm = new LoginViewModel(App.AuthService);
        _vm.LoginSucceeded += OnLoginSucceeded;
        UpdateUI();
    }

    private void UpdateUI()
    {
        TitleText.Text = _vm.TitleText;
        SubtitleText.Text = _vm.SubtitleText;
        SubmitButton.Content = _vm.SubmitText;
        ToggleButton.Content = _vm.ToggleText;
        RegisterFields.Visibility = _vm.IsRegisterMode ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void SubmitClick(object sender, RoutedEventArgs e)
    {
        SubmitButton.IsEnabled = false;
        HideMessages();

        _vm.Email = EmailBox.Text;
        _vm.FirstName = FirstNameBox.Text;
        _vm.LastName = LastNameBox.Text;
        _vm.Phone = PhoneBox.Text;

        await _vm.SubmitCommand.ExecuteAsync(PasswordBox.Password);

        if (!string.IsNullOrEmpty(_vm.ErrorMessage))
        {
            ErrorText.Text = _vm.ErrorMessage;
            ErrorBorder.Visibility = Visibility.Visible;
        }
        SubmitButton.IsEnabled = true;
    }

    private void ToggleClick(object sender, RoutedEventArgs e)
    {
        _vm.ToggleModeCommand.Execute(null);
        HideMessages();
        UpdateUI();
    }

    private void OnLoginSucceeded()
    {
        var main = new MainWindow();
        main.Show();
        Close();
    }

    private void HideMessages()
    {
        ErrorBorder.Visibility = Visibility.Collapsed;
        SuccessBorder.Visibility = Visibility.Collapsed;
    }

    private void CloseClick(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    private void MinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void DragWindow(object sender, MouseButtonEventArgs e) => DragMove();
}
