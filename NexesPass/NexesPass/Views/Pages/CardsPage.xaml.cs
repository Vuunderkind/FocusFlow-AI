using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NexesPass.Converters;
using NexesPass.Models;
using NexesPass.ViewModels;

namespace NexesPass.Views.Pages;

public partial class CardsPage : Page
{
    private readonly CardsViewModel _vm;
    private BankCard? _selectedCard;
    private string _selectedColor = "#6C63FF";

    public CardsPage()
    {
        InitializeComponent();
        Resources["CardStatusTxt"] = new CardStatusTextConverter();
        _vm = new CardsViewModel(App.CardService, App.AccountService);
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _vm.LoadCommand.ExecuteAsync(null);
        CardsList.ItemsSource = _vm.Cards;
        EmptyCards.Visibility = _vm.Cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        CardAccountBox.ItemsSource = _vm.Accounts;
        if (_vm.Accounts.Count > 0)
            CardAccountBox.SelectedIndex = 0;

        if (_vm.SelectedCard != null)
            DisplayCard(_vm.SelectedCard);
    }

    private void SelectCard(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is BankCard card)
            DisplayCard(card);
    }

    private void DisplayCard(BankCard card)
    {
        _selectedCard = card;
        NoCardSelected.Visibility = Visibility.Collapsed;
        CardDetail.Visibility = Visibility.Visible;

        bool isFrozen = card.Status == CardStatus.Frozen;
        StatusText.Text = card.Status switch
        {
            CardStatus.Active => "Активна",
            CardStatus.Frozen => "Заморожена",
            CardStatus.Blocked => "Заблокирована",
            _ => ""
        };
        StatusDot.Fill = card.Status switch
        {
            CardStatus.Active => new SolidColorBrush(Color.FromRgb(0x51, 0xCF, 0x66)),
            CardStatus.Frozen => new SolidColorBrush(Color.FromRgb(0x74, 0xC0, 0xFC)),
            _ => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B))
        };
        FreezeBtn.Content = isFrozen ? "Разморозить карту" : "Заморозить карту";
        DailyLimitText.Text = $"{card.DailyLimit:N0} ₽";
        MonthlyLimitText.Text = $"{card.MonthlyLimit:N0} ₽";
        MsgText.Visibility = Visibility.Collapsed;
    }

    private async void FreezeCard(object sender, RoutedEventArgs e)
    {
        if (_selectedCard == null) return;
        await _vm.FreezeCardCommand.ExecuteAsync(_selectedCard.Id);
        MsgText.Text = _vm.SuccessMessage;
        MsgText.Foreground = new SolidColorBrush(Color.FromRgb(0x51, 0xCF, 0x66));
        MsgText.Visibility = Visibility.Visible;
        await LoadAsync();
    }

    private void ShowIssueForm(object sender, RoutedEventArgs e) =>
        IssueForm.Visibility = Visibility.Visible;
    private void HideIssueForm(object sender, RoutedEventArgs e) =>
        IssueForm.Visibility = Visibility.Collapsed;

    private void SelectColor(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement el)
            _selectedColor = el.Tag?.ToString() ?? "#6C63FF";
    }

    private async void ExecuteIssue(object sender, RoutedEventArgs e)
    {
        _vm.SelectedAccountForCard = CardAccountBox.SelectedItem as Models.Account;
        _vm.SelectedCardType = CardTypeBox.SelectedIndex switch { 1 => "Credit", 2 => "Virtual", _ => "Debit" };
        _vm.SelectedNetwork = CardNetworkBox.SelectedIndex switch { 1 => "Mastercard", _ => CardNetworkBox.SelectedIndex == 0 ? "Visa" : "Mir" };
        _vm.SelectedColor = _selectedColor;

        await _vm.IssueCardCommand.ExecuteAsync(null);

        if (!string.IsNullOrEmpty(_vm.ErrorMessage))
        {
            IssueErrText.Text = _vm.ErrorMessage;
            IssueErrBorder.Visibility = Visibility.Visible;
        }
        else
        {
            IssueForm.Visibility = Visibility.Collapsed;
            IssueErrBorder.Visibility = Visibility.Collapsed;
            await LoadAsync();
        }
    }
}
