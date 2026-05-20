namespace CentralBillingService.WPF.Views;

public partial class GlobalInvoicesView : UserControl
{
    public GlobalInvoicesView()
    {
        InitializeComponent();
    }

    private void Row_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is GlobalInvoicesViewModel vm)
            vm.PreviewInvoiceCommand.Execute(vm.SelectedInvoice);
    }
}
