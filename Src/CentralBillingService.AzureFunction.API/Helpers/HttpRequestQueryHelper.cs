namespace CentralBillingService.AzureFunction.API.Helpers;

internal static class HttpRequestQueryHelper
{
    public static TValue GetRequiredQueryValue<TValue>(HttpRequest req, string key)
    {
        IQueryCollection query = req.Query;

        if (query.TryGetValue(key, out StringValues values))
        {
            string value = values.ToString();
            var result = ConvertValue<TValue>(value);

            if (result != null)
            {
                return result;
            }

            throw new ArgumentException($"Cannot convert value '{value}' to type {typeof(TValue).Name}");
        }

        throw new ArgumentException($"Required parameter '{key}' not found in query string");
    }

    public static List<TValue> GetQueryValues<TValue>(HttpRequest req, string key)
    {
        IQueryCollection query = req.Query;
        var results = new List<TValue>();

        if (query.TryGetValue(key, out StringValues values))
        {
            foreach (var value in values)
            {
                var converted = ConvertValue<TValue>(value);
                if (converted != null)
                {
                    results.Add(converted);
                }
            }
        }

        return results;
    }

    private static TValue? ConvertValue<TValue>(string value)
    {
        Type targetType = typeof(TValue);

        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (underlyingType == typeof(string))
            {
                return (TValue)(object)value;
            }

            // Enums
            if (underlyingType.IsEnum)
            {
                if (Enum.TryParse(underlyingType, value, true, out object? enumValue))
                {
                    return (TValue)enumValue;
                }
                return default;
            }

            // Boolean (from "1", "0", "true", "false", "yes", "no")
            if (underlyingType == typeof(bool))
            {
                if (value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return (TValue)(object)true;
                }
                if (value.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    value.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return (TValue)(object)false;
                }
            }

            if (underlyingType == typeof(Guid))
            {
                if (Guid.TryParse(value, out Guid guidValue))
                {
                    return (TValue)(object)guidValue;
                }
                return default;
            }

            object converted = Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            return (TValue)converted;
        }
        catch
        {
            return default;
        }
    }

    public static TValue GetRequestedModel<TValue>(HttpRequestData req)
        where TValue : class, new()
    {
        TValue model = new();
        Type modelType = typeof(TValue);
        PropertyInfo[] properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);

        if (query != null && query.AllKeys.Length > 0)
        {
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                var value = query[property.Name];
                if (!string.IsNullOrWhiteSpace(value))
                {
                    object? convertedValue = ConvertValueLegacy(property.PropertyType, value);

                    if (convertedValue != null)
                    {
                        property.SetValue(model, convertedValue);
                    }
                }
            }
        }

        return model;
    }

    private static object? ConvertValueLegacy(Type targetType, string value)
    {
        object? result = null;

        if (targetType == typeof(string))
        {
            result = value;
        }
        else if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, value, true, out object? enumValue))
            {
                result = enumValue;
            }
        }
        else
        {
            try
            {
                result = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                // Do nothing by design
            }
        }

        return result;
    }
}