using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NexesPass.Models;
using NexesPass.ViewModels;

namespace NexesPass.Views.Pages;

public partial class TransferPage : Page
{
    private readonly TransferViewModel _vm;
    private bool _usePhone;

    public TransferPage()
    {
        InitializeComponent();
        _vm = new TransferViewModel(App.TransactionService, App.AccountService);
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _vm.LoadCommand.ExecuteAsync(null);
        FromAccountBox.ItemsSource = _vm.Accounts;
        if (_vm.SelectedFromAccount != null)
        {
            FromAccountBox.SelectedItem = _vm.SelectedFromAccount;
            UpdateAvailable(_vm.SelectedFromAccount);
        }
    }

    private void FromAccountChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FromAccountBox.SelectedItem is Account acc)
            UpdateAvailable(acc);
    }

    private void UpdateAvailable(Account acc)
    {
        AvailableText.Text = $"{acc.AvailableBalance:N2} {acc.Currency}";
        CurrencySymbol.Text = acc.Currency switch
        {
            Currency.USD => "$",
            Currency.EUR => "€",
            Currency.GBP => "£",
            _ => "₽"
        };
    }

    private void SelectByAccount(object sender, RoutedEventArgs e)
    {
        _usePhone = false;
        ByAccountBtn.Style = (Style)FindResource("PrimaryButton");
        ByPhoneBtn.Style = (Style)FindResource("GhostButton");
        RecipientLabel.Text = "СЧЁТ ПОЛУЧАТЕЛЯ";
        RecipientBox.SetValue(TextBox.TextProperty, "");
        RecipientBox.GetType().GetProperty("PlaceholderText")?.SetValue(RecipientBox, "Номер счёта получателя");
    }

    private void SelectByPhone(object sender, RoutedEventArgs e)
    {
        _usePhone = true;
        ByPhoneBtn.Style = (Style)FindResource("PrimaryButton");
        ByAccountBtn.Style = (Style)FindResource("GhostButton");
        RecipientLabel.Text = "ТЕЛЕФОН ПОЛУЧАТЕЛЯ";
        RecipientBox.SetValue(TextBox.TextProperty, "");
    }

    private void QuickAmount(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tag)
            AmountBox.Text = tag;
    }

    private async void ExecuteTransfer(object sender, RoutedEventArgs e)
    {
        HideMessages();
        TransferBtn.IsEnabled = false;

        _vm.SelectedFromAccount = FromAccountBox.SelectedItem as Account;
        _vm.UsePhone = _usePhone;
        _vm.Amount = decimal.TryParse(AmountBox.Text, out decimal a) ? a : 0;
        _vm.Description = DescriptionBox.Text;

        if (_usePhone)
            _vm.RecipientPhone = RecipientBox.Text;
        else
            _vm.ToAccountNumber = RecipientBox.Text;

        await _vm.ExecuteTransferCommand.ExecuteAsync(null);

        if (!string.IsNullOrEmpty(_vm.ErrorMessage))
        {
            ErrText.Text = _vm.ErrorMessage;
            ErrBorder.Visibility = Visibility.Visible;
        }
        else if (!string.IsNullOrEmpty(_vm.SuccessMessage))
        {
            SuccText.Text = _vm.SuccessMessage;
            SuccBorder.Visibility = Visibility.Visible;
            AmountBox.Text = "0";
            RecipientBox.Text = "";
            DescriptionBox.Text = "";

            // Refresh accounts
            await LoadAsync();
        }

        TransferBtn.IsEnabled = true;
    }

    private void HideMessages()
    {
        ErrBorder.Visibility = Visibility.Collapsed;
        SuccBorder.Visibility = Visibility.Collapsed;
    }
}
