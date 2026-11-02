using CentralBillingService.WPF.Services;

namespace CentralBillingService.WPF.ViewModels;

public partial class BillingSourcesViewModel : ObservableObject
{
    private readonly AppSettingsService _service;
    private readonly Action _onSaved;

    // ── List ─────────────────────────────────────────────────────────────────
    [ObservableProperty] ObservableCollection<BillingSourceRecord> sources = [];
    [ObservableProperty] BillingSourceRecord? selectedSource;
    [ObservableProperty] bool isEditing;
    [ObservableProperty] bool isNewRecord;

    // ── Edit fields ──────────────────────────────────────────────────────────
    [ObservableProperty] string editKey                = "";
    [ObservableProperty] string editSecret             = "";
    [ObservableProperty] string editLegalName          = "";
    [ObservableProperty] string editTradeName          = "";
    [ObservableProperty] string editTaxIdValue         = "";
    [ObservableProperty] string editTaxIdCountryCode   = "ES";
    [ObservableProperty] string editEmail              = "";
    [ObservableProperty] string editPhone              = "";
    [ObservableProperty] string editWebsite            = "";
    [ObservableProperty] string editAddressLine1       = "";
    [ObservableProperty] string editAddressLine2       = "";
    [ObservableProperty] string editCity               = "";
    [ObservableProperty] string editProvince           = "";
    [ObservableProperty] string editPostalCode         = "";
    [ObservableProperty] string editAddressCountryCode = "ES";
    [ObservableProperty] string editDefaultLayout      = InvoiceLayoutNames.Invoice;
    [ObservableProperty] string? editError;

    public IReadOnlyList<string> LayoutOptions { get; } = InvoiceLayoutNames.All;

    public BillingSourcesViewModel(AppSettingsService service, Action onSaved)
    {
        _service = service;
        _onSaved = onSaved;
        Sources = new(_service.LoadBillingSources());
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    [RelayCommand]
    void NewSource()
    {
        SelectedSource = null;
        IsNewRecord = true;
        EditKey = EditSecret = EditLegalName = EditTradeName = "";
        EditTaxIdValue = "";
        EditTaxIdCountryCode = "ES";
        EditEmail = EditPhone = EditWebsite = "";
        EditAddressLine1 = EditAddressLine2 = EditCity = EditProvince = EditPostalCode = "";
        EditAddressCountryCode = "ES";
        EditDefaultLayout = InvoiceLayoutNames.Invoice;
        EditSecret = AccessTokenGenerator.GenerateAccessToken();
        EditError = null;
        IsEditing = true;
    }

    [RelayCommand]
    void EditSource()
    {
        if (SelectedSource is null) return;
        var s = SelectedSource;
        IsNewRecord = false;
        EditKey                = s.Key;
        EditSecret             = s.Secret;
        EditLegalName          = s.LegalName;
        EditTradeName          = s.TradeName          ?? "";
        EditTaxIdValue         = s.TaxIdValue;
        EditTaxIdCountryCode   = s.TaxIdCountryCode;
        EditEmail              = s.Email;
        EditPhone              = s.Phone              ?? "";
        EditWebsite            = s.Website            ?? "";
        EditAddressLine1       = s.AddressLine1;
        EditAddressLine2       = s.AddressLine2       ?? "";
        EditCity               = s.City;
        EditProvince           = s.Province           ?? "";
        EditPostalCode         = s.PostalCode;
        EditAddressCountryCode = s.AddressCountryCode;
        EditDefaultLayout      = InvoiceLayoutNames.Resolve(null, s.DefaultLayout);
        EditError = null;
        IsEditing = true;
    }

    [RelayCommand]
    void SaveSource()
    {
        EditError = null;
        if (string.IsNullOrWhiteSpace(EditKey))
        { EditError = "El identificador (clave única) es obligatorio."; return; }
        if (string.IsNullOrWhiteSpace(EditSecret))
        { EditError = "El secreto (token de autenticación) es obligatorio."; return; }
        if (string.IsNullOrWhiteSpace(EditLegalName))
        { EditError = "El nombre fiscal es obligatorio."; return; }
        if (string.IsNullOrWhiteSpace(EditTaxIdValue) &&
            EditTaxIdCountryCode.Trim().Equals("ES", StringComparison.OrdinalIgnoreCase))
        { EditError = "El NIF/VAT es obligatorio para entidades españolas."; return; }
        if (string.IsNullOrWhiteSpace(EditEmail) || !EditEmail.Contains('@'))
        { EditError = "El email debe ser válido."; return; }
        if (string.IsNullOrWhiteSpace(EditAddressLine1))
        { EditError = "La dirección es obligatoria."; return; }
        if (string.IsNullOrWhiteSpace(EditCity))
        { EditError = "La ciudad es obligatoria."; return; }
        if (string.IsNullOrWhiteSpace(EditPostalCode))
        { EditError = "El código postal es obligatorio."; return; }

        var key = EditKey.Trim().ToLowerInvariant();
        if (IsNewRecord && Sources.Any(s => s.Key == key))
        { EditError = $"Ya existe una fuente con el identificador '{key}'."; return; }

        var existing = Sources.FirstOrDefault(s => s.Key == key);
        if (existing is null)
        {
            existing = new BillingSourceRecord { Key = key };
            Sources.Add(existing);
        }

        existing.Secret             = EditSecret.Trim();
        existing.LegalName          = EditLegalName.Trim();
        existing.TradeName          = string.IsNullOrWhiteSpace(EditTradeName)          ? null : EditTradeName.Trim();
        existing.TaxIdValue         = EditTaxIdValue.Trim().ToUpperInvariant();
        existing.TaxIdCountryCode   = EditTaxIdCountryCode.Trim().ToUpperInvariant();
        existing.Email              = EditEmail.Trim();
        existing.Phone              = string.IsNullOrWhiteSpace(EditPhone)              ? null : EditPhone.Trim();
        existing.Website            = string.IsNullOrWhiteSpace(EditWebsite)            ? null : EditWebsite.Trim();
        existing.AddressLine1       = EditAddressLine1.Trim();
        existing.AddressLine2       = string.IsNullOrWhiteSpace(EditAddressLine2)       ? null : EditAddressLine2.Trim();
        existing.City               = EditCity.Trim();
        existing.Province           = string.IsNullOrWhiteSpace(EditProvince)           ? null : EditProvince.Trim();
        existing.PostalCode         = EditPostalCode.Trim();
        existing.AddressCountryCode = EditAddressCountryCode.Trim().ToUpperInvariant();
        existing.DefaultLayout      = InvoiceLayoutNames.Resolve(null, EditDefaultLayout);

        _service.SaveBillingSources(Sources);
        RefreshList(Sources, existing);
        IsEditing = false;
        SelectedSource = null;
        _onSaved();
    }

    [RelayCommand]
    void DeleteSource()
    {
        if (SelectedSource is null) return;
        Sources.Remove(SelectedSource);
        _service.SaveBillingSources(Sources);
        IsEditing = false;
        SelectedSource = null;
        _onSaved();
    }

    [RelayCommand]
    void GenerateSecret()
    {
        bool replaceConfirmed = IsNewRecord || string.IsNullOrWhiteSpace(EditSecret);
        if (!replaceConfirmed)
        {
            MessageBoxResult answer = MessageBox.Show(
                "Se generará un nuevo token para esta fuente. Los clientes que usen el token actual dejarán de autenticarse hasta que se les entregue el nuevo.\n\n¿Continuar?",
                "Generar nuevo token", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            replaceConfirmed = answer == MessageBoxResult.Yes;
        }

        if (replaceConfirmed)
        {
            EditSecret = AccessTokenGenerator.GenerateAccessToken();
        }
    }

    [RelayCommand]
    void CopySecret()
    {
        if (!string.IsNullOrWhiteSpace(EditSecret))
        {
            Clipboard.SetText(EditSecret);
        }
    }

    [RelayCommand]
    void CancelEdit()
    {
        IsEditing = false;
        SelectedSource = null;
        EditError = null;
    }

    private static void RefreshList<T>(ObservableCollection<T> list, T item)
    {
        var idx = list.IndexOf(item);
        if (idx >= 0) { list.RemoveAt(idx); list.Insert(idx, item); }
    }
}
