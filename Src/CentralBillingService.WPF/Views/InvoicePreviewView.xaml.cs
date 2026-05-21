using System.ComponentModel;

namespace CentralBillingService.WPF.Views;

public partial class InvoicePreviewView : UserControl
{
    public InvoicePreviewView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await PdfViewer.EnsureCoreWebView2Async();
        if (DataContext is InvoicePreviewViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            NavigateToPdf(vm.PdfTempPath);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is InvoicePreviewViewModel vm)
            vm.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InvoicePreviewViewModel.PdfTempPath) &&
            sender is InvoicePreviewViewModel vm)
        {
            Dispatcher.Invoke(() => NavigateToPdf(vm.PdfTempPath));
        }
    }

    private void NavigateToPdf(string? path)
    {
        if (path is null || PdfViewer.CoreWebView2 is null) return;
        PdfViewer.CoreWebView2.Navigate(new Uri(path).AbsoluteUri);
    }
}
