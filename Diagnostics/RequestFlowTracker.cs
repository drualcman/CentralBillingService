using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Aboitiz.Power.MobileAp.Core.Data.Diagnostics;
using Gluonics.Core.IoC;

namespace Aboitiz.Power.MobileAp.Core.Services.Diagnostics;

[AutoRegister<IRequestFlowTracker>]
internal sealed class RequestFlowTracker : IRequestFlowTracker
{
    private readonly IRequestFlowContextAccessor _contextAccessor;
    private readonly IRequestFlowStorage _storage;

    private readonly ConcurrentDictionary<Guid, Stopwatch> _queries = [];

    private readonly string _instanceId;
#nullable enable
    private Stopwatch? _stopwatch;

    private string? _operationId;

    private bool _started;
    private bool _ended;

    public string? CurrentOperationId =>
        _operationId;

    public RequestFlowTracker(
        IRequestFlowContextAccessor contextAccessor,
        IRequestFlowStorage storage)
    {
        _contextAccessor = contextAccessor;
        _storage = storage;

        _instanceId = Guid.NewGuid().ToString("N");
    }

    public void Start(
        string? operationId = null,
        object? data = null,
        bool overwriteCurrent = false,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        _ = EnsureStarted(
            operationId,
            overwriteCurrent,
            methodName,
            filePath,
            lineNumber,
            data);
    }

    public void Track(
        string message,
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string finalOperationId =
            EnsureStarted(
                operationId,
                false,
                methodName,
                filePath,
                lineNumber);

        TrackInternal(
            finalOperationId,
            "TRACK",
            message,
            data,
            null,
            methodName,
            filePath,
            lineNumber);
    }

    public void TrackException(
        Exception exception,
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string finalOperationId =
            EnsureStarted(
                operationId,
                false,
                methodName,
                filePath,
                lineNumber);

        TrackInternal(
            finalOperationId,
            "EXCEPTION",
            exception.Message,
            data,
            exception,
            methodName,
            filePath,
            lineNumber);
    }

    public Guid QueryStart(
        string queryName,
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string finalOperationId =
            EnsureStarted(
                operationId,
                false,
                methodName,
                filePath,
                lineNumber);

        Guid queryId = Guid.NewGuid();

        Stopwatch stopwatch = Stopwatch.StartNew();

        _queries.TryAdd(
            queryId,
            stopwatch);

        TrackInternal(
            finalOperationId,
            "QUERY_START",
            queryName,
            new
            {
                QueryId = queryId,
                Data = data
            },
            null,
            methodName,
            filePath,
            lineNumber);

        return queryId;
    }

    public void QueryEnd(
        Guid queryId,
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string finalOperationId =
            EnsureStarted(
                operationId,
                false,
                methodName,
                filePath,
                lineNumber);

        bool found =
            _queries.TryRemove(
                queryId,
                out Stopwatch? stopwatch);

        if (found && stopwatch is not null)
        {
            stopwatch.Stop();

            TrackInternal(
                finalOperationId,
                "QUERY_END",
                "Query finished",
                new
                {
                    QueryId = queryId,
                    DurationMs = stopwatch.ElapsedMilliseconds,
                    Data = data
                },
                null,
                methodName,
                filePath,
                lineNumber);
        }
        else
        {
            TrackInternal(
                finalOperationId,
                "QUERY_END_UNKNOWN",
                "Unknown query",
                new
                {
                    QueryId = queryId,
                    Data = data
                },
                null,
                methodName,
                filePath,
                lineNumber);
        }
    }

    public void End(
        object? data = null,
        string? operationId = null,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        if (_ended)
        {
            return;
        }

        string finalOperationId =
            EnsureStarted(
                operationId,
                false,
                methodName,
                filePath,
                lineNumber);

        _ended = true;

        if (_stopwatch is not null)
        {
            _stopwatch.Stop();
        }

        TrackInternal(
            finalOperationId,
            "END",
            "Tracking finished",
            data,
            null,
            methodName,
            filePath,
            lineNumber);
    }

    public ValueTask DisposeAsync()
    {
        if (_started &&
            _ended == false)
        {
            End(
                "Auto Dispose",
                _operationId);
        }

        return ValueTask.CompletedTask;
    }

    private string EnsureStarted(
        string? operationId,
        bool overwriteCurrent,
        string methodName,
        string filePath,
        int lineNumber,
        object? startData = null)
    {
        string finalOperationId =
            ResolveOperationId(
                operationId,
                overwriteCurrent);
        string message;
        bool autoStarted;
        if (_started == false)
        {
            _started = true;

            _stopwatch = Stopwatch.StartNew();
            autoStarted = true;
            message = "Tracking started";
        }
        else
        {
            autoStarted = false;
            message = "Tracking re-started";
        }

        if (_started == false || overwriteCurrent == true)
        {
            TrackInternal(
                finalOperationId,
                "START",
                message,
                startData ?? new
                {
                    AutoStarted = autoStarted,
                    OverwriteCurrent = overwriteCurrent
                },
                null,
                methodName,
                filePath,
                lineNumber);
        }

        return finalOperationId;
    }

    private string ResolveOperationId(
        string? operationId,
        bool overwriteCurrent = false)
    {
        if (string.IsNullOrWhiteSpace(operationId) == false)
        {
            _operationId = operationId;
        }

        if (string.IsNullOrWhiteSpace(_operationId))
        {
            string? currentOperationId =
                _contextAccessor.CurrentOperationId;

            if (string.IsNullOrWhiteSpace(currentOperationId) == false)
            {
                _operationId = currentOperationId;
            }
        }

        if (string.IsNullOrWhiteSpace(_operationId))
        {
            _operationId = $"AUTO_{Guid.NewGuid()}";
        }

        if (overwriteCurrent ||
            string.IsNullOrWhiteSpace(
                _contextAccessor.CurrentOperationId))
        {
            _contextAccessor.CurrentOperationId =
                _operationId;
        }

        return _operationId;
    }

    private void TrackInternal(
        string operationId,
        string eventType,
        string message,
        object? data,
        Exception? exception,
        string methodName,
        string filePath,
        int lineNumber)
    {
        Process currentProcess =
            Process.GetCurrentProcess();

        RequestFlowEntry entry = new()
        {
            OperationId = operationId,
            InstanceId = _instanceId,
            TimestampUtc = DateTime.UtcNow,
            EventType = eventType,
            Message = message,
            MethodName = methodName,
            FilePath = filePath,
            LineNumber = lineNumber,
            ElapsedMilliseconds =
                _stopwatch?.ElapsedMilliseconds ?? 0,
            Data = data,
            Exception = exception?.ToString(),
            CpuUsage =
                currentProcess.TotalProcessorTime.TotalMilliseconds,
            WorkingSetBytes =
                currentProcess.WorkingSet64,
            ManagedMemoryBytes =
                GC.GetTotalMemory(false),
            ThreadCount =
                currentProcess.Threads.Count,
            ThreadId =
                Environment.CurrentManagedThreadId
        };

        _storage.AppendAsync(entry)
            .Wait();
    }
}