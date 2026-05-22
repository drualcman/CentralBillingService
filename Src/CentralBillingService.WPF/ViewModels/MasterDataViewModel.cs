using CentralBillingService.WPF.Services;

namespace CentralBillingService.WPF.ViewModels;

public partial class MasterDataViewModel : ObservableObject
{
    private readonly LocalMasterDataStore _store;

    // ── Collections ──────────────────────────────────────────────────────────
    [ObservableProperty] ObservableCollection<ClientRecord>  clients  = [];
    [ObservableProperty] ObservableCollection<SeriesRecord>  series   = [];
    [ObservableProperty] ObservableCollection<ProductRecord> products = [];
    [ObservableProperty] ObservableCollection<NoteRecord>    notes    = [];

    // ── Selected items ───────────────────────────────────────────────────────
    [ObservableProperty] ClientRecord?  selectedClient;
    [ObservableProperty] SeriesRecord?  selectedSeries;
    [ObservableProperty] ProductRecord? selectedProduct;
    [ObservableProperty] NoteRecord?    selectedNote;

    // ── Edit panels visibility ────────────────────────────────────────────────
    [ObservableProperty] bool isEditingClient;
    [ObservableProperty] bool isEditingSeries;
    [ObservableProperty] bool isEditingProduct;
    [ObservableProperty] bool isEditingNote;

    // ── Client edit fields ────────────────────────────────────────────────────
    [ObservableProperty] Guid   editClientId;
    [ObservableProperty] string editClientLegalName   = "";
    [ObservableProperty] string editClientTradeName   = "";
    [ObservableProperty] string editClientTaxId       = "";
    [ObservableProperty] string editClientTaxIdCountry = "ES";
    [ObservableProperty] string editClientEmail       = "";
    [ObservableProperty] string editClientPhone       = "";
    [ObservableProperty] string editClientAddress     = "";
    [ObservableProperty] string editClientCity        = "";
    [ObservableProperty] string editClientPostalCode  = "";
    [ObservableProperty] string editClientProvince    = "";
    [ObservableProperty] string editClientCountry     = "ES";
    [ObservableProperty] string? editClientExternalId;

    // ── Series edit fields ────────────────────────────────────────────────────
    [ObservableProperty] Guid   editSeriesId;
    [ObservableProperty] string editSeriesCode        = "";
    [ObservableProperty] string editSeriesDescription = "";

    // ── Product edit fields ───────────────────────────────────────────────────
    [ObservableProperty] Guid    editProductId;
    [ObservableProperty] string  editProductCode         = "";
    [ObservableProperty] string  editProductDescription  = "";
    [ObservableProperty] decimal editProductUnitPrice;
    [ObservableProperty] decimal editProductTaxRate      = 21;
    [ObservableProperty] string  editProductCurrencyCode = "";

    // ── Note edit fields ──────────────────────────────────────────────────────
    [ObservableProperty] Guid   editNoteId;
    [ObservableProperty] string editNoteName    = "";
    [ObservableProperty] string editNoteContent = "";

    public MasterDataViewModel(LocalMasterDataStore store)
    {
        _store = store;
        Reload();
    }

    private void Reload()
    {
        Clients  = new(_store.LoadClients());
        Series   = new(_store.LoadSeries());
        Products = new(_store.LoadProducts());
        Notes    = new(_store.LoadNotes());
    }

    // ── Client commands ───────────────────────────────────────────────────────

    [RelayCommand]
    void NewClient()
    {
        SelectedClient = null;
        EditClientId = Guid.NewGuid();
        EditClientLegalName = EditClientTradeName = EditClientTaxId = "";
        EditClientTaxIdCountry = "ES";
        EditClientEmail = EditClientPhone = EditClientAddress = "";
        EditClientCity = EditClientPostalCode = EditClientProvince = "";
        EditClientCountry = "ES";
        EditClientExternalId = null;
        IsEditingClient = true;
    }

    [RelayCommand]
    void EditClient()
    {
        if (SelectedClient is null) return;
        var c = SelectedClient;
        EditClientId          = c.Id;
        EditClientLegalName   = c.LegalName;
        EditClientTradeName   = c.TradeName   ?? "";
        EditClientTaxId       = c.TaxId;
        EditClientTaxIdCountry = c.TaxIdCountry;
        EditClientEmail       = c.Email       ?? "";
        EditClientPhone       = c.Phone       ?? "";
        EditClientAddress     = c.Address     ?? "";
        EditClientCity        = c.City        ?? "";
        EditClientPostalCode  = c.PostalCode  ?? "";
        EditClientProvince    = c.Province    ?? "";
        EditClientCountry     = c.Country     ?? "ES";
        EditClientExternalId  = c.ExternalId;
        IsEditingClient = true;
    }

    [ObservableProperty] string? clientEditError;

    [RelayCommand]
    void SaveClient()
    {
        ClientEditError = null;
        if (string.IsNullOrWhiteSpace(EditClientLegalName))
        { ClientEditError = "El nombre fiscal es obligatorio."; return; }
        if (string.IsNullOrWhiteSpace(EditClientTaxId))
        { ClientEditError = "El NIF/VAT es obligatorio."; return; }
        if (string.IsNullOrWhiteSpace(EditClientEmail) || !EditClientEmail.Contains('@'))
        { ClientEditError = "El email es obligatorio y debe ser válido."; return; }
        if (string.IsNullOrWhiteSpace(EditClientAddress))
        { ClientEditError = "La dirección es obligatoria."; return; }
        if (string.IsNullOrWhiteSpace(EditClientCity))
        { ClientEditError = "La ciudad es obligatoria."; return; }
        if (string.IsNullOrWhiteSpace(EditClientPostalCode))
        { ClientEditError = "El código postal es obligatorio."; return; }

        var existing = Clients.FirstOrDefault(c => c.Id == EditClientId);
        if (existing is null)
        {
            existing = new ClientRecord { Id = EditClientId };
            Clients.Add(existing);
        }

        existing.LegalName    = EditClientLegalName.Trim();
        existing.TradeName    = string.IsNullOrWhiteSpace(EditClientTradeName) ? null : EditClientTradeName.Trim();
        existing.TaxId        = EditClientTaxId.Trim().ToUpperInvariant();
        existing.TaxIdCountry = EditClientTaxIdCountry.Trim().ToUpperInvariant();
        existing.Email        = string.IsNullOrWhiteSpace(EditClientEmail)      ? null : EditClientEmail.Trim();
        existing.Phone        = string.IsNullOrWhiteSpace(EditClientPhone)      ? null : EditClientPhone.Trim();
        existing.Address      = string.IsNullOrWhiteSpace(EditClientAddress)    ? null : EditClientAddress.Trim();
        existing.City         = string.IsNullOrWhiteSpace(EditClientCity)       ? null : EditClientCity.Trim();
        existing.PostalCode   = string.IsNullOrWhiteSpace(EditClientPostalCode) ? null : EditClientPostalCode.Trim();
        existing.Province     = string.IsNullOrWhiteSpace(EditClientProvince)   ? null : EditClientProvince.Trim();
        existing.Country      = string.IsNullOrWhiteSpace(EditClientCountry)    ? null : EditClientCountry.Trim().ToUpperInvariant();
        existing.ExternalId   = string.IsNullOrWhiteSpace(EditClientExternalId) ? null : EditClientExternalId.Trim();

        _store.SaveClients(Clients.ToList());
        RefreshList(Clients, existing);
        IsEditingClient = false;
        SelectedClient = null;
    }

    [RelayCommand]
    void DeleteClient()
    {
        if (SelectedClient is null) return;
        Clients.Remove(SelectedClient);
        _store.SaveClients(Clients.ToList());
        IsEditingClient = false;
        SelectedClient = null;
    }

    [RelayCommand]
    void CancelClientEdit() { IsEditingClient = false; SelectedClient = null; ClientEditError = null; }

    // ── Series commands ───────────────────────────────────────────────────────

    [RelayCommand]
    void NewSeries()
    {
        SelectedSeries = null;
        EditSeriesId = Guid.NewGuid();
        EditSeriesCode = EditSeriesDescription = "";
        IsEditingSeries = true;
    }

    [RelayCommand]
    void EditSeries()
    {
        if (SelectedSeries is null) return;
        EditSeriesId          = SelectedSeries.Id;
        EditSeriesCode        = SelectedSeries.Code;
        EditSeriesDescription = SelectedSeries.Description ?? "";
        IsEditingSeries = true;
    }

    [RelayCommand]
    void SaveSeries()
    {
        if (string.IsNullOrWhiteSpace(EditSeriesCode)) return;

        var existing = Series.FirstOrDefault(s => s.Id == EditSeriesId);
        if (existing is null)
        {
            existing = new SeriesRecord { Id = EditSeriesId };
            Series.Add(existing);
        }

        existing.Code        = EditSeriesCode.Trim().ToUpperInvariant();
        existing.Description = string.IsNullOrWhiteSpace(EditSeriesDescription) ? null : EditSeriesDescription.Trim();

        _store.SaveSeries(Series.ToList());
        RefreshList(Series, existing);
        IsEditingSeries = false;
        SelectedSeries = null;
    }

    [RelayCommand]
    void DeleteSeries()
    {
        if (SelectedSeries is null) return;
        Series.Remove(SelectedSeries);
        _store.SaveSeries(Series.ToList());
        IsEditingSeries = false;
        SelectedSeries = null;
    }

    [RelayCommand]
    void CancelSeriesEdit() { IsEditingSeries = false; SelectedSeries = null; }

    // ── Product commands ──────────────────────────────────────────────────────

    [RelayCommand]
    void NewProduct()
    {
        SelectedProduct = null;
        EditProductId = Guid.NewGuid();
        EditProductCode = EditProductDescription = EditProductCurrencyCode = "";
        EditProductUnitPrice = 0;
        EditProductTaxRate = 21;
        IsEditingProduct = true;
    }

    [RelayCommand]
    void EditProduct()
    {
        if (SelectedProduct is null) return;
        var p = SelectedProduct;
        EditProductId           = p.Id;
        EditProductCode         = p.Code;
        EditProductDescription  = p.Description;
        EditProductUnitPrice    = p.DefaultUnitPrice;
        EditProductTaxRate      = p.DefaultTaxRate;
        EditProductCurrencyCode = p.DefaultCurrencyCode ?? "";
        IsEditingProduct = true;
    }

    [RelayCommand]
    void SaveProduct()
    {
        if (string.IsNullOrWhiteSpace(EditProductDescription)) return;

        var existing = Products.FirstOrDefault(p => p.Id == EditProductId);
        if (existing is null)
        {
            existing = new ProductRecord { Id = EditProductId };
            Products.Add(existing);
        }

        existing.Code                = EditProductCode.Trim().ToUpperInvariant();
        existing.Description         = EditProductDescription.Trim();
        existing.DefaultUnitPrice    = EditProductUnitPrice;
        existing.DefaultTaxRate      = EditProductTaxRate;
        existing.DefaultCurrencyCode = string.IsNullOrWhiteSpace(EditProductCurrencyCode)
            ? null : EditProductCurrencyCode.Trim().ToUpperInvariant();

        _store.SaveProducts(Products.ToList());
        RefreshList(Products, existing);
        IsEditingProduct = false;
        SelectedProduct = null;
    }

    [RelayCommand]
    void DeleteProduct()
    {
        if (SelectedProduct is null) return;
        Products.Remove(SelectedProduct);
        _store.SaveProducts(Products.ToList());
        IsEditingProduct = false;
        SelectedProduct = null;
    }

    [RelayCommand]
    void CancelProductEdit() { IsEditingProduct = false; SelectedProduct = null; }

    // ── Note commands ─────────────────────────────────────────────────────────

    [RelayCommand]
    void NewNote()
    {
        SelectedNote = null;
        EditNoteId = Guid.NewGuid();
        EditNoteName = EditNoteContent = "";
        IsEditingNote = true;
    }

    [RelayCommand]
    void EditNote()
    {
        if (SelectedNote is null) return;
        EditNoteId      = SelectedNote.Id;
        EditNoteName    = SelectedNote.Name;
        EditNoteContent = SelectedNote.Content;
        IsEditingNote = true;
    }

    [ObservableProperty] string? noteEditError;

    [RelayCommand]
    void SaveNote()
    {
        NoteEditError = null;
        if (string.IsNullOrWhiteSpace(EditNoteName))
        { NoteEditError = "El nombre es obligatorio."; return; }

        var existing = Notes.FirstOrDefault(n => n.Id == EditNoteId);
        if (existing is null)
        {
            existing = new NoteRecord { Id = EditNoteId };
            Notes.Add(existing);
        }

        existing.Name    = EditNoteName.Trim();
        existing.Content = EditNoteContent.Trim();

        _store.SaveNotes(Notes.ToList());
        RefreshList(Notes, existing);
        IsEditingNote = false;
        SelectedNote = null;
    }

    [RelayCommand]
    void DeleteNote()
    {
        if (SelectedNote is null) return;
        Notes.Remove(SelectedNote);
        _store.SaveNotes(Notes.ToList());
        IsEditingNote = false;
        SelectedNote = null;
    }

    [RelayCommand]
    void CancelNoteEdit() { IsEditingNote = false; SelectedNote = null; NoteEditError = null; }

    // ── Helper ────────────────────────────────────────────────────────────────

    // Forces the ListBox/DataGrid to re-render the row after in-place mutation
    private static void RefreshList<T>(ObservableCollection<T> list, T item)
    {
        var idx = list.IndexOf(item);
        if (idx >= 0) { list.RemoveAt(idx); list.Insert(idx, item); }
    }
}
