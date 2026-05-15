using System.Globalization;
using System.Windows.Data;

namespace CentralBillingService.WPF.Converters;

public sealed class StatusToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var status = value as string ?? "";
        return status.ToLowerInvariant() switch
        {
            "issued"    => "Emitida",
            "rectified" => "Rectificada",
            "cancelled" => "Anulada",
            "draft"     => "Borrador",
            _           => status,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
