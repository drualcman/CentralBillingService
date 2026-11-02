using System.Text.Json.Nodes;

namespace CentralBillingService.WPF.Services;

public class AppSettingsService
{
    private readonly string SettingsPath =
        Path.Combine(AppContext.BaseDirectory, "appsettings.json");

    public List<BillingSourceRecord> LoadBillingSources()
    {
        JsonNode root = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        JsonArray array = root["CbsOptions"]?["BillingSources"]?.AsArray() ?? [];
        List<BillingSourceRecord> result = [];

        foreach (JsonNode? source in array)
        {
            if (source is not null)
            {
                result.Add(ToRecord(source));
            }
        }

        return result;
    }

    public void SaveBillingSources(IEnumerable<BillingSourceRecord> records)
    {
        JsonNode root = JsonNode.Parse(File.ReadAllText(SettingsPath))!;
        JsonArray previousSources = root["CbsOptions"]?["BillingSources"]?.AsArray() ?? [];

        JsonArray array = new JsonArray();
        foreach (BillingSourceRecord record in records)
        {
            JsonObject source = FindSourceClone(previousSources, record.Key) ?? new JsonObject();
            ApplyRecord(source, record);
            array.Add(source);
        }

        root["CbsOptions"]!["BillingSources"] = array;
        File.WriteAllText(SettingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static BillingSourceRecord ToRecord(JsonNode source)
    {
        JsonNode? issuer = source["Issuer"];
        return new BillingSourceRecord
        {
            Key                = source["BillingSource"]?.GetValue<string>()      ?? "",
            Secret             = source["Secret"]?.GetValue<string>()             ?? "",
            DefaultLayout      = source["DefaultLayout"]?.GetValue<string>(),
            LegalName          = issuer?["LegalName"]?.GetValue<string>()         ?? "",
            TradeName          = issuer?["TradeName"]?.GetValue<string>(),
            TaxIdValue         = issuer?["TaxIdValue"]?.GetValue<string>()        ?? "",
            TaxIdCountryCode   = issuer?["TaxIdCountryCode"]?.GetValue<string>()  ?? "ES",
            Email              = issuer?["Email"]?.GetValue<string>()             ?? "",
            Phone              = issuer?["Phone"]?.GetValue<string>(),
            Website            = issuer?["Website"]?.GetValue<string>(),
            AddressLine1       = issuer?["AddressLine1"]?.GetValue<string>()      ?? "",
            AddressLine2       = issuer?["AddressLine2"]?.GetValue<string>(),
            City               = issuer?["City"]?.GetValue<string>()              ?? "",
            Province           = issuer?["Province"]?.GetValue<string>(),
            PostalCode         = issuer?["PostalCode"]?.GetValue<string>()        ?? "",
            AddressCountryCode = issuer?["AddressCountryCode"]?.GetValue<string>() ?? "ES",
        };
    }

    private static JsonObject? FindSourceClone(JsonArray sources, string key)
    {
        JsonNode? match = sources.FirstOrDefault(source =>
            string.Equals(source?["BillingSource"]?.GetValue<string>(), key, StringComparison.OrdinalIgnoreCase));
        return match?.DeepClone().AsObject();
    }

    private static void ApplyRecord(JsonObject source, BillingSourceRecord record)
    {
        source["BillingSource"] = record.Key;
        source["Secret"]        = record.Secret;
        SetOrRemove(source, "DefaultLayout", record.DefaultLayout);

        JsonObject issuer = source["Issuer"] as JsonObject ?? new JsonObject();
        issuer["LegalName"]          = record.LegalName;
        SetOrRemove(issuer, "TradeName", record.TradeName);
        issuer["TaxIdValue"]         = record.TaxIdValue;
        issuer["TaxIdCountryCode"]   = record.TaxIdCountryCode;
        issuer["Email"]              = record.Email;
        SetOrRemove(issuer, "Phone", record.Phone);
        SetOrRemove(issuer, "Website", record.Website);
        issuer["AddressLine1"]       = record.AddressLine1;
        SetOrRemove(issuer, "AddressLine2", record.AddressLine2);
        issuer["City"]               = record.City;
        SetOrRemove(issuer, "Province", record.Province);
        issuer["PostalCode"]         = record.PostalCode;
        issuer["AddressCountryCode"] = record.AddressCountryCode;
        source["Issuer"] = issuer;
    }

    private static void SetOrRemove(JsonObject target, string propertyName, string? value)
    {
        if (value is null)
        {
            target.Remove(propertyName);
        }
        else
        {
            target[propertyName] = value;
        }
    }
}
