namespace CentralBillingService.WPF.Models;

public sealed class InvoiceLineItem : ObservableObject
{
    private string _description = string.Empty;
    private int _quantity = 1;
    private decimal _unitPrice;
    private int _taxRate = 21;
    private ProductRecord? _selectedProduct;

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set => SetProperty(ref _unitPrice, value);
    }

    public int TaxRate
    {
        get => _taxRate;
        set => SetProperty(ref _taxRate, value);
    }

    public ProductRecord? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value) && value is not null)
            {
                Description = value.Description;
                UnitPrice = value.DefaultUnitPrice;
                TaxRate = (int)value.DefaultTaxRate;
            }
        }
    }

    public static int[] TaxRates { get; } = [0, 4, 10, 21];
}
