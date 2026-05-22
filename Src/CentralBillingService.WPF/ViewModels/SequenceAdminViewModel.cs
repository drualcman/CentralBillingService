using CentralBillingService.Domain.Services;
using CentralBillingService.Persistence.SqlServer.Admin;

namespace CentralBillingService.WPF.ViewModels;

public partial class SequenceAdminViewModel : ObservableObject
{
    private readonly ISequenceAdminService _service;

    // ── Collections ──────────────────────────────────────────────────────────
    [ObservableProperty] ObservableCollection<SequenceInfo> sequences = [];
    public IReadOnlyList<string> BillingSources { get; }

    // ── State ─────────────────────────────────────────────────────────────────
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isInitializing;
    [ObservableProperty] string? errorMessage;
    [ObservableProperty] string? successMessage;

    // ── Init form fields ──────────────────────────────────────────────────────
    [ObservableProperty] string initBillingSource = "";
    [ObservableProperty] string initSerie = "";
    [ObservableProperty] int initYear = DateTime.Now.Year;
    [ObservableProperty] int initStartAt = 1;

    public SequenceAdminViewModel(ISequenceAdminService service, IReadOnlyList<string> billingSources)
    {
        _service = service;
        BillingSources = billingSources;
        if (billingSources.Count > 0)
            InitBillingSource = billingSources[0];
    }

    [RelayCommand]
    async Task LoadAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var list = await _service.GetAllAsync();
            Sequences = new(list);
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    void OpenInit()
    {
        InitSerie = "";
        InitYear = DateTime.Now.Year;
        InitStartAt = 1;
        ErrorMessage = null;
        SuccessMessage = null;
        IsInitializing = true;
    }

    [RelayCommand]
    async Task SaveInitAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(InitBillingSource))
        { ErrorMessage = "Selecciona un BillingSource."; return; }
        if (string.IsNullOrWhiteSpace(InitSerie))
        { ErrorMessage = "El código de serie es obligatorio."; return; }
        if (InitYear < 2026)
        { ErrorMessage = "El año debe ser 2026 o posterior."; return; }
        if (InitStartAt < 1)
        { ErrorMessage = "El número de inicio debe ser al menos 1."; return; }

        try
        {
            var serie = InitSerie.Trim().ToUpperInvariant();
            await _service.InitializeAsync(InitBillingSource, serie, InitYear, InitStartAt);
            SuccessMessage = $"Secuencia {serie}/{InitYear} lista. La próxima factura recibirá el número {InitStartAt:D4}.";
            IsInitializing = false;
            await LoadAsync();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }

    [RelayCommand]
    void CancelInit()
    {
        IsInitializing = false;
        ErrorMessage = null;
    }

    [RelayCommand]
    async Task DeleteSequenceAsync(SequenceInfo? seq)
    {
        if (seq is null) return;
        ErrorMessage = null;
        SuccessMessage = null;
        try
        {
            await _service.DeleteAsync(seq.BillingSource, seq.Serie, seq.Year);
            Sequences.Remove(seq);
            SuccessMessage = $"Secuencia {seq.Serie}/{seq.Year} eliminada.";
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
    }
}
