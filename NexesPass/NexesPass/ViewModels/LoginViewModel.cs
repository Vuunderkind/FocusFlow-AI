using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NexesPass.Services;

namespace NexesPass.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly AuthService _auth;

    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _firstName = string.Empty;
    [ObservableProperty] private string _lastName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private bool _isRegisterMode;
    [ObservableProperty] private string _toggleText = "Нет аккаунта? Зарегистрироваться";
    [ObservableProperty] private string _submitText = "Войти";
    [ObservableProperty] private string _titleText = "Добро пожаловать";
    [ObservableProperty] private string _subtitleText = "Войдите в свой аккаунт Nexes Pass";

    public event Action? LoginSucceeded;

    public LoginViewModel(AuthService auth) => _auth = auth;

    [RelayCommand]
    public void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ClearMessages();
        if (IsRegisterMode)
        {
            ToggleText = "Уже есть аккаунт? Войти";
            SubmitText = "Создать аккаунт";
            TitleText = "Создать аккаунт";
            SubtitleText = "Присоединитесь к Nexes Pass сегодня";
        }
        else
        {
            ToggleText = "Нет аккаунта? Зарегистрироваться";
            SubmitText = "Войти";
            TitleText = "Добро пожаловать";
            SubtitleText = "Войдите в свой аккаунт Nexes Pass";
        }
    }

    [RelayCommand]
    public async Task SubmitAsync(string password)
    {
        ClearMessages();
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
        {
            SetError("Заполните все поля");
            return;
        }

        IsBusy = true;
        try
        {
            if (IsRegisterMode)
            {
                if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
                {
                    SetError("Введите имя и фамилию");
                    return;
                }
                var (ok, err) = await _auth.RegisterAsync(
                    FirstName, LastName, Email, Phone, password, "1234");
                if (ok)
                    LoginSucceeded?.Invoke();
                else
                    SetError(err);
            }
            else
            {
                var (ok, err) = await _auth.LoginAsync(Email, password);
                if (ok)
                    LoginSucceeded?.Invoke();
                else
                    SetError(err);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
