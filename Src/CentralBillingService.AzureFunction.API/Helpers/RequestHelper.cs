namespace CentralBillingService.AzureFunction.API.Helpers;

internal static class RequestHelper
{
    public static string GetSecret(HttpRequestData req) =>
        req.Headers.TryGetValues("x-cbs-key", out var values)
            ? values.FirstOrDefault() ?? "Unknown"
            : "Unknown";
    public static string GetBillingSource(HttpRequestData req) =>
        req.Headers.TryGetValues("x-cbs-billing-source", out var values)
            ? values.FirstOrDefault() ?? "Unknown"
            : "Unknown";
    public static string GetRequestLanguage(HttpRequestData req) =>
        req.Headers.TryGetValues("Accept-Language", out var values)
            ? values.FirstOrDefault() ?? "Unknown"
            : "Unknown";

    public static string GetBrowser(HttpRequestData req) =>
        req.Headers.TryGetValues("User-Agent", out var values)
            ? values.FirstOrDefault() ?? "Unknown"
            : "Unknown";

    public static string GetClientIpAddress(HttpRequestData req, FunctionContext context)
    {
        // 1. Primero intentar con las cabeceras comunes
        string ipAddress = GetIpFromHeaders(req);

        // 2. Si no se encuentra, intentar obtenerla desde el contexto de la función
        if (ipAddress == "Unknown")
        {
            ipAddress = GetIpFromFunctionContext(context);
        }

        // 3. Si sigue siendo Unknown, intentar desde las propiedades del request
        if (ipAddress == "Unknown")
        {
            ipAddress = GetIpFromRequestProperties(req);
        }

        return ipAddress;
    }

    private static string GetIpFromHeaders(HttpRequestData req)
    {
        // X-Forwarded-For (más común en Azure y proxies)
        if (req.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
        {
            string forwardedIp = forwardedFor.FirstOrDefault()?.Split(',')[0].Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(forwardedIp) && forwardedIp != "Unknown")
                return forwardedIp;
        }

        // X-Azure-ClientIP (específico de Azure)
        if (req.Headers.TryGetValues("X-Azure-ClientIP", out var azureClientIp))
        {
            string azureIp = azureClientIp.FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(azureIp) && azureIp != "Unknown")
                return azureIp;
        }

        // CLIENT-IP
        if (req.Headers.TryGetValues("CLIENT-IP", out var clientIpHeader))
        {
            string clientIp = clientIpHeader.FirstOrDefault()?.Split(':')[0].Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(clientIp) && clientIp != "Unknown")
                return clientIp;
        }

        // X-Client-IP
        if (req.Headers.TryGetValues("X-Client-IP", out var clientXIp))
        {
            string headerIp = clientXIp.FirstOrDefault() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(headerIp) && headerIp != "Unknown")
                return headerIp;
        }

        return "Unknown";
    }

    private static string GetIpFromFunctionContext(FunctionContext context)
    {
        try
        {
            // Obtener el binding context
            var bindingContext = context.GetHttpContext();

            // Intentar obtener la IP desde las características del contexto
            if (context.Items.TryGetValue("HttpRequestData", out var httpRequestData) &&
                httpRequestData is HttpRequestData req)
            {
                // Reintentar con headers desde el contexto
                return GetIpFromHeaders(req);
            }

            // En isolated worker, a veces la IP está en los items del contexto
            if (context.Items.TryGetValue("RemoteIpAddress", out var remoteIp) && remoteIp is string ipString)
                return ipString;

            if (context.Items.TryGetValue("RemoteIpAddress", out remoteIp) && remoteIp is IPAddress ip)
                return ip.ToString();
        }
        catch
        {
            // Si falla, continuar
        }

        return "Unknown";
    }

    private static string GetIpFromRequestProperties(HttpRequestData req)
    {
        try
        {
            // Usar reflexión para acceder a propiedades internas
            var requestType = req.GetType();

            // Intentar obtener RemoteIpAddress mediante reflexión
            var remoteIpProperty = requestType.GetProperty("RemoteIpAddress");
            if (remoteIpProperty != null)
            {
                var ipValue = remoteIpProperty.GetValue(req);
                if (ipValue is IPAddress ip)
                    return ip.ToString();
                if (ipValue is string ipString && !string.IsNullOrWhiteSpace(ipString))
                    return ipString;
            }

            // Intentar obtener ConnectionInfo
            var connectionInfoProperty = requestType.GetProperty("ConnectionInfo");
            if (connectionInfoProperty != null)
            {
                var connectionInfo = connectionInfoProperty.GetValue(req);
                if (connectionInfo != null)
                {
                    var remoteIpProp = connectionInfo.GetType().GetProperty("RemoteIpAddress");
                    if (remoteIpProp != null)
                    {
                        var ipValue = remoteIpProp.GetValue(connectionInfo);
                        if (ipValue is IPAddress ip)
                            return ip.ToString();
                        if (ipValue is string ipString && !string.IsNullOrWhiteSpace(ipString))
                            return ipString;
                    }
                }
            }
        }
        catch
        {
            // Si falla la reflexión, devolver Unknown
        }

        return "Unknown";
    }

    public static string GetRequestOrigin(HttpRequestData req)
    {
        // Primero intentar con Origin
        if (req.Headers.TryGetValues("Origin", out var origin))
        {
            var originValue = origin.FirstOrDefault();
            if (!string.IsNullOrEmpty(originValue))
                return originValue;
        }

        // Fallback a Referer
        if (req.Headers.TryGetValues("Referer", out var referer))
        {
            var refererValue = referer.FirstOrDefault();
            if (!string.IsNullOrEmpty(refererValue))
                return refererValue;
        }

        return "Unknown";
    }
}