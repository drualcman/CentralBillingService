namespace Aboitiz.Power.MobileAp.Core.Data.Diagnostics;

public interface IRequestFlowContextAccessor
{
#nullable enable
    string? CurrentOperationId { get; set; }
}
