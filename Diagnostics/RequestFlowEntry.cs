namespace Aboitiz.Power.MobileAp.Core.Data.Diagnostics;

public sealed class RequestFlowEntry
{
    public required string OperationId { get; init; }

    public required string InstanceId { get; init; }

    public required DateTime TimestampUtc { get; init; }

    public required string EventType { get; init; }

    public required string Message { get; init; }

    public required string MethodName { get; init; }

    public required string FilePath { get; init; }

    public required int LineNumber { get; init; }

    public required long ElapsedMilliseconds { get; init; }
#nullable enable
    public object? Data { get; init; }

    public string? Exception { get; init; }

    public double CpuUsage { get; init; }

    public long WorkingSetBytes { get; init; }

    public long ManagedMemoryBytes { get; init; }

    public int ThreadCount { get; init; }

    public int ThreadId { get; init; }
}