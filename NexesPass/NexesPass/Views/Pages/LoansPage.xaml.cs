using System.Windows;
using System.Windows.Controls;
using NexesPass.Converters;
using NexesPass.Models;
using NexesPass.ViewModels;

namespace NexesPass.Views.Pages;

public partial class LoansPage : Page
{
    private readonly LoansViewModel _vm;

    public LoansPage()
    {
        InitializeComponent();
        Resources["LoanTypeConv"] = new LoanTypeConverter();
        Resources["LoanStatusConv"] = new LoanStatusConverter();
        Resources["ProgConv"] = new ProgressWidthConverter();
        _vm = new LoansViewModel(App.LoanService, App.AccountService);
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _vm.LoadCommand.ExecuteAsync(null);
        LoansList.ItemsSource = _vm.Loans;
        EmptyLoans.Visibility = _vm.Loans.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ApplyAccountBox.ItemsSource = _vm.Accounts;
        if (_vm.Accounts.Count > 0)
            ApplyAccountBox.SelectedIndex = 0;
        UpdateCalc();
    }

    private void RecalcCalc(object sender, RoutedEventArgs e) => UpdateCalc();
    private void RecalcCalc(object sender, SelectionChangedEventArgs e) => UpdateCalc();

    private void UpdateCalc()
    {
        if (!decimal.TryParse(CalcAmountBox?.Text, out decimal amount)) amount = 100000;
        if (!int.TryParse(CalcTermBox?.Text, out int term)) term = 24;

        decimal rate = CalcTypeBox?.SelectedIndex switch
        {
            1 => 8.5m, 2 => 11.5m, 3 => 16.5m, _ => 14.9m
        };

        var monthly = LoanService.CalculateMonthlyPayment(amount, rate, term);
        var total = monthly * term;
        var overpay = total - amount;

        if (CalcMonthly != null) CalcMonthly.Text = $"{monthly:N2} ₽";
        if (CalcTotal != null) CalcTotal.Text = $"{total:N2} ₽";
        if (CalcOverpay != null) CalcOverpay.Text = $"{overpay:N2} ₽";
    }

    private async void MakePayment(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is int loanId)
        {
            await _vm.MakePaymentCommand.ExecuteAsync(loanId);
            if (!string.IsNullOrEmpty(_vm.ErrorMessage))
            {
                MsgText.Text = _vm.ErrorMessage;
                MsgBorder.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x2D, 0x15, 0x15));
                MsgBorder.Visibility = Visibility.Visible;
            }
            else if (!string.IsNullOrEmpty(_vm.SuccessMessage))
            {
                MsgText.Text = _vm.SuccessMessage;
                MsgBorder.Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x15, 0x2D, 0x1A));
                MsgText.Foreground = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(0x51, 0xCF, 0x66));
                MsgBorder.Visibility = Visibility.Visible;
            }
        }
    }

    private void ShowApplyForm(object sender, RoutedEventArgs e) =>
        ApplyForm.Visibility = Visibility.Visible;
    private void HideApplyForm(object sender, RoutedEventArgs e) =>
        ApplyForm.Visibility = Visibility.Collapsed;

    private async void ExecuteApply(object sender, RoutedEventArgs e)
    {
        _vm.SelectedAccount = ApplyAccountBox.SelectedItem as Account;
        _vm.SelectedLoanType = ApplyTypeBox.SelectedIndex switch
        { 1 => "Mortgage", 2 => "Auto", 3 => "Business", _ => "Personal" };

        if (!decimal.TryParse(ApplyAmountBox.Text, out decimal amount))
        {
            ApplyErrText.Text = "Введите корректную сумму";
            ApplyErrBorder.Visibility = Visibility.Visible;
            return;
        }
        if (!int.TryParse(ApplyTermBox.Text, out int term))
        {
            ApplyErrText.Text = "Введите корректный срок";
            ApplyErrBorder.Visibility = Visibility.Visible;
            return;
        }

        _vm.LoanAmount = amount;
        _vm.TermMonths = term;
        _vm.Purpose = ApplyPurposeBox.Text;

        await _vm.ApplyCommand.ExecuteAsync(null);

        if (!string.IsNullOrEmpty(_vm.ErrorMessage))
        {
            ApplyErrText.Text = _vm.ErrorMessage;
            ApplyErrBorder.Visibility = Visibility.Visible;
        }
        else
        {
            ApplyForm.Visibility = Visibility.Collapsed;
            ApplyErrBorder.Visibility = Visibility.Collapsed;
            await LoadAsync();
        }
    }
}

public class ProgressWidthConverter : System.Windows.Data.IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (value is decimal pct)
            return Math.Max(4, (double)pct * 3.0); // Scale to approx width
        return 4.0;
    }
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        => throw new NotImplementedException();
}
