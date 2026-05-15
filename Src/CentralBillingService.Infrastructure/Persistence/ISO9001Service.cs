namespace CentralBillingService.Infrastructure.Persistence;

internal class Iso9001Service(
    IRegisterAuditLog auditLog,
    IRegisterIncidentReport incidentReport,
    ILogger<Iso9001Service> logger) : IIso9001
{
    public async Task Register<T, TData>(string reference, T action, string description, TData data)
    {
        try
        {
            string safeReference = string.IsNullOrWhiteSpace(reference)
                ? Guid.NewGuid().ToString()
                : reference;

            await auditLog.HandleAsync(
                new AuditLogDto(
                    safeReference,
                    "CentralBillingService",
                    typeof(T).FullName,
                    "system",
                    DateTime.UtcNow,
                    description,
                    JsonSerializer.Serialize(data)));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[{reference}] {action}: {description}",
                reference,
                typeof(T).Name,
                description);

            logger.LogError(JsonSerializer.Serialize(data));
        }
    }

    public async Task Register<T>(string reference, T action, string description)
    {
        try
        {
            string safeReference = string.IsNullOrWhiteSpace(reference)
                ? Guid.NewGuid().ToString()
                : reference;

            await auditLog.HandleAsync(
                new AuditLogDto(
                    safeReference,
                    "CentralBillingService",
                    typeof(T).FullName,
                    "system",
                    DateTime.UtcNow,
                    description,
                    ""));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[{reference}] {action}: {description}",
                reference,
                typeof(T).Name,
                description);
        }
    }

    public async Task Error<T>(string reference, T action, Exception ex)
    {
        ex.Data["IsLogged"] = true;
        try
        {
            string safeReference = string.IsNullOrWhiteSpace(reference)
                ? Guid.NewGuid().ToString()
                : reference;

            await incidentReport.HandleAsync(
                new IncidentReportDto(
                    "CentralBillingService",
                    safeReference,
                    DateTime.UtcNow,
                    "system",
                    ex.Message,
                    typeof(T).FullName,
                    "exception",
                    ex.ToString()));
        }
        catch (Exception internalEx)
        {
            logger.LogError(internalEx,
                "[{reference}] {action}: {description}",
                reference,
                typeof(T).Name,
                ex.Message);
        }
    }

    public async Task Error<T>(string reference, T action, string description)
    {
        try
        {
            string safeReference = string.IsNullOrWhiteSpace(reference)
                ? Guid.NewGuid().ToString()
                : reference;

            await incidentReport.HandleAsync(
                new IncidentReportDto(
                    "CentralBillingService",
                    safeReference,
                    DateTime.UtcNow,
                    "system",
                    description,
                    typeof(T).FullName,
                    "exception",
                    ""));
        }
        catch (Exception ex)
        {
            ex.Data["IsLogged"] = true;
            logger.LogError(ex,
                "[{reference}] {action}: {description}",
                reference,
                typeof(T).Name,
                description);
        }
    }

    public async Task Error<T, TData>(string reference, T action, string description, TData data)
    {
        try
        {
            string safeReference = string.IsNullOrWhiteSpace(reference)
                ? Guid.NewGuid().ToString()
                : reference;

            await incidentReport.HandleAsync(
                new IncidentReportDto(
                    "CentralBillingService",
                    safeReference,
                    DateTime.UtcNow,
                    "system",
                    description,
                    typeof(T).FullName,
                    "exception",
                    JsonSerializer.Serialize(data)));
        }
        catch (Exception ex)
        {
            ex.Data["IsLogged"] = true;
            logger.LogError(ex,
                "[{reference}] {action}: {description}",
                reference,
                typeof(T).Name,
                description);

            logger.LogError(JsonSerializer.Serialize(data));
        }
    }
}