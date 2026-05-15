using System.Globalization;
using System.Windows.Data;

namespace CentralBillingService.WPF.Converters;

public sealed class NullToVisibilityConverter : IValueConverter
{
    public bool InvertWhenNull { get; set; }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value is null;
        bool show = InvertWhenNull ? isNull : !isNull;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
