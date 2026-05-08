using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NexesPass.Models;

namespace NexesPass.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter as string == "invert";
        bool val = value is bool b && b;
        if (invert) val = !val;
        return val ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Visible;
}

public class AmountColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Green = new(Color.FromRgb(0x51, 0xCF, 0x66));
    private static readonly SolidColorBrush Red = new(Color.FromRgb(0xFF, 0x6B, 0x6B));
    private static readonly SolidColorBrush White = new(Colors.White);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return d >= 0 ? Green : Red;
        return White;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CurrencySymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Currency c ? c switch
        {
            Currency.RUB => "₽",
            Currency.USD => "$",
            Currency.EUR => "€",
            Currency.GBP => "£",
            Currency.CNY => "¥",
            _ => ""
        } : "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class AccountTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is AccountType t ? t switch
        {
            AccountType.Checking => "Текущий",
            AccountType.Savings => "Накопительный",
            AccountType.Investment => "Инвестиционный",
            _ => ""
        } : "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CardStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is CardStatus s ? s switch
        {
            CardStatus.Active => new SolidColorBrush(Color.FromRgb(0x51, 0xCF, 0x66)),
            CardStatus.Frozen => new SolidColorBrush(Color.FromRgb(0x74, 0xC0, 0xFC)),
            CardStatus.Blocked => new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B)),
            _ => new SolidColorBrush(Colors.Gray)
        } : new SolidColorBrush(Colors.Gray);
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class CardStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is CardStatus s ? s switch
        {
            CardStatus.Active => "Активна",
            CardStatus.Frozen => "Заморожена",
            CardStatus.Blocked => "Заблокирована",
            CardStatus.Expired => "Истёкла",
            _ => ""
        } : "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TransactionTypeIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is TransactionType t ? t switch
        {
            TransactionType.Transfer => "↔",
            TransactionType.Payment => "💳",
            TransactionType.TopUp => "↓",
            TransactionType.Withdrawal => "↑",
            TransactionType.CardPayment => "💳",
            TransactionType.LoanPayment => "🏦",
            TransactionType.Interest => "📈",
            _ => "•"
        } : "•";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class LoanStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is LoanStatus s ? s switch
        {
            LoanStatus.Active => "Активный",
            LoanStatus.PaidOff => "Погашен",
            LoanStatus.Overdue => "Просрочен",
            LoanStatus.Rejected => "Отклонён",
            LoanStatus.Pending => "Рассматривается",
            _ => ""
        } : "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class LoanTypeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is LoanType t ? t switch
        {
            LoanType.Personal => "Потребительский",
            LoanType.Mortgage => "Ипотека",
            LoanType.Auto => "Автокредит",
            LoanType.Business => "Бизнес",
            _ => ""
        } : "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string fmt && value != null)
            return string.Format(fmt, value);
        return value?.ToString() ?? "";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool invert = parameter as string == "invert";
        bool isNull = value == null;
        return (invert ? !isNull : isNull) ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
