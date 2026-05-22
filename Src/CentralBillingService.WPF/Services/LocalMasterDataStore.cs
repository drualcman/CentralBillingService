namespace CentralBillingService.WPF.Services;

public class LocalMasterDataStore
{
    private static readonly JsonSerializerOptions _json = new() { WriteIndented = true };
    private readonly string _dir;

    public LocalMasterDataStore()
    {
        _dir = Path.Combine(
            AppContext.BaseDirectory,
            "DATA");
        Directory.CreateDirectory(_dir);
    }

    public List<ClientRecord> LoadClients() => Load<ClientRecord>("clients.json");
    public List<SeriesRecord> LoadSeries() => Load<SeriesRecord>("series.json");
    public List<ProductRecord> LoadProducts() => Load<ProductRecord>("products.json");
    public List<NoteRecord> LoadNotes() => Load<NoteRecord>("notes.json");

    public void SaveClients(List<ClientRecord> items) => Save("clients.json", items);
    public void SaveSeries(List<SeriesRecord> items) => Save("series.json", items);
    public void SaveProducts(List<ProductRecord> items) => Save("products.json", items);
    public void SaveNotes(List<NoteRecord> items) => Save("notes.json", items);

    private List<T> Load<T>(string file)
    {
        var path = Path.Combine(_dir, file);
        if (!File.Exists(path))
            return [];
        return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(path), _json) ?? [];
    }

    private void Save<T>(string file, List<T> items) =>
        File.WriteAllText(Path.Combine(_dir, file), JsonSerializer.Serialize(items, _json));
}
