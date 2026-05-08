using CommunityToolkit.Mvvm.ComponentModel;

namespace NexesPass.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _successMessage = string.Empty;

    protected void SetError(string msg)
    {
        ErrorMessage = msg;
        SuccessMessage = string.Empty;
    }

    protected void SetSuccess(string msg)
    {
        SuccessMessage = msg;
        ErrorMessage = string.Empty;
    }

    protected void ClearMessages()
    {
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }
}
