using System.Windows.Controls;
using System.Windows.Input;

namespace CentralBillingService.WPF.Views;

public partial class InvoicesView : UserControl
{
    public InvoicesView()
    {
        InitializeComponent();
    }

    private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is InvoicesViewModel vm && vm.SelectedInvoice is not null)
            vm.ViewDetailCommand.Execute(vm.SelectedInvoice);
    }
}
