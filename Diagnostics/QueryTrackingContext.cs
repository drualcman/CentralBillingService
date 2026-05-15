using System.Diagnostics;

namespace Aboitiz.Power.MobileAp.Core.Services.Diagnostics;

internal sealed class QueryTrackingContext
{
    public required Guid QueryId { get; init; }

    public required string QueryName { get; init; }

    public required Stopwatch Stopwatch { get; init; }

    public bool Completed { get; set; }
}