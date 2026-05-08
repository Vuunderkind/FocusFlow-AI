using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexesPass.Models;
using NexesPass.Services;

namespace NexesPass.ViewModels;

public partial class CardsViewModel : BaseViewModel
{
    private readonly CardService _cardService;
    private readonly AccountService _accountService;

    [ObservableProperty] private List<BankCard> _cards = new();
    [ObservableProperty] private List<Account> _accounts = new();
    [ObservableProperty] private BankCard? _selectedCard;
    [ObservableProperty] private bool _showIssueForm;
    [ObservableProperty] private Account? _selectedAccountForCard;
    [ObservableProperty] private string _selectedCardType = "Debit";
    [ObservableProperty] private string _selectedNetwork = "Mir";
    [ObservableProperty] private string _selectedColor = "#6C63FF";

    public CardsViewModel(CardService cardService, AccountService accountService)
    {
        _cardService = cardService;
        _accountService = accountService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        Cards = await _cardService.GetUserCardsAsync();
        Accounts = await _accountService.GetUserAccountsAsync();
        SelectedCard = Cards.FirstOrDefault();
    }

    [RelayCommand]
    public async Task IssueCardAsync()
    {
        if (SelectedAccountForCard == null)
        {
            SetError("Выберите счёт для выпуска карты");
            return;
        }

        IsBusy = true;
        try
        {
            var type = Enum.Parse<CardType>(SelectedCardType);
            var network = Enum.Parse<CardNetwork>(SelectedNetwork);

            var (ok, err, card) = await _cardService.IssueCardAsync(
                SelectedAccountForCard.Id, type, network, SelectedColor);

            if (ok)
            {
                SetSuccess("Карта успешно выпущена!");
                ShowIssueForm = false;
                await LoadAsync();
            }
            else SetError(err);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task FreezeCardAsync(int cardId)
    {
        if (SelectedCard == null) return;
        bool freeze = SelectedCard.Status == CardStatus.Active;
        await _cardService.FreezeCardAsync(cardId, freeze);
        await LoadAsync();
        SelectedCard = Cards.FirstOrDefault(c => c.Id == cardId);
        SetSuccess(freeze ? "Карта заморожена" : "Карта разморожена");
    }
}
