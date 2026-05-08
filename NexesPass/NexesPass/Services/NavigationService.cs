using System.Windows.Controls;

namespace NexesPass.Services;

public class NavigationService
{
    private Frame? _frame;

    public void Initialize(Frame frame) => _frame = frame;

    public void Navigate(Page page)
    {
        _frame?.Navigate(page);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }
}
