using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace CentralBillingService.WPF.Converters;

public sealed class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value as string ?? "";
        return status.ToLowerInvariant() switch
        {
            "issued"      => new SolidColorBrush(Color.FromRgb(16, 185, 129)),   // green
            "rectified"   => new SolidColorBrush(Color.FromRgb(245, 158, 11)),   // amber
            "cancelled"   => new SolidColorBrush(Color.FromRgb(239, 68, 68)),    // red
            _             => new SolidColorBrush(Color.FromRgb(148, 163, 184)),  // slate
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
