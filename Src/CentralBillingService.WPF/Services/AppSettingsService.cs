using System.Text.Json.Nodes;

namespace CentralBillingService.WPF.Services;

public class AppSettingsService
{
    private readonly string _path =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public List<BillingSourceRecord> LoadBillingSources()
    {
        var json = File.ReadAllText(_path);
        var root = JsonNode.Parse(json)!;
        var array = root["CbsOptions"]?["BillingSources"]?.AsArray() ?? [];
        var result = new List<BillingSourceRecord>();

        foreach (var s in array)
        {
            if (s is null) continue;
            var issuer = s["Issuer"];
            result.Add(new BillingSourceRecord
            {
                Key                = s["BillingSource"]?.GetValue<string>()      ?? "",
                Secret             = s["Secret"]?.GetValue<string>()              ?? "",
                LegalName          = issuer?["LegalName"]?.GetValue<string>()     ?? "",
                TradeName          = issuer?["TradeName"]?.GetValue<string>(),
                TaxIdValue         = issuer?["TaxIdValue"]?.GetValue<string>()    ?? "",
                TaxIdCountryCode   = issuer?["TaxIdCountryCode"]?.GetValue<string>() ?? "ES",
                Email              = issuer?["Email"]?.GetValue<string>()         ?? "",
                Phone              = issuer?["Phone"]?.GetValue<string>(),
                Website            = issuer?["Website"]?.GetValue<string>(),
                AddressLine1       = issuer?["AddressLine1"]?.GetValue<string>()  ?? "",
                City               = issuer?["City"]?.GetValue<string>()          ?? "",
                PostalCode         = issuer?["PostalCode"]?.GetValue<string>()    ?? "",
                AddressCountryCode = issuer?["AddressCountryCode"]?.GetValue<string>() ?? "ES",
            });
        }

        return result;
    }

    public void SaveBillingSources(IEnumerable<BillingSourceRecord> records)
    {
        var json = File.ReadAllText(_path);
        var root = JsonNode.Parse(json)!;

        var array = new JsonArray();
        foreach (var r in records)
        {
            array.Add(new JsonObject
            {
                ["BillingSource"] = r.Key,
                ["Secret"]        = r.Secret,
                ["Issuer"]        = new JsonObject
                {
                    ["LegalName"]          = r.LegalName,
                    ["TradeName"]          = r.TradeName,
                    ["TaxIdValue"]         = r.TaxIdValue,
                    ["TaxIdCountryCode"]   = r.TaxIdCountryCode,
                    ["Email"]              = r.Email,
                    ["Phone"]              = r.Phone,
                    ["Website"]            = r.Website,
                    ["AddressLine1"]       = r.AddressLine1,
                    ["City"]               = r.City,
                    ["PostalCode"]         = r.PostalCode,
                    ["AddressCountryCode"] = r.AddressCountryCode,
                },
            });
        }

        root["CbsOptions"]!["BillingSources"] = array;
        File.WriteAllText(_path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
