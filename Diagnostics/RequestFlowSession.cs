using System.Diagnostics;

namespace Aboitiz.Power.MobileAp.Core.Services.Diagnostics;

internal sealed class RequestFlowSession
{
    public required string OperationId { get; init; }

    public required DateTime StartedUtc { get; init; }

    public required Stopwatch Stopwatch { get; init; }

    public bool Started { get; set; }

    public bool Ended { get; set; }

    public bool HasErrors { get; set; }
#nullable enable
    public string? RootMethodName { get; set; }

    public int StartCount { get; set; }
}