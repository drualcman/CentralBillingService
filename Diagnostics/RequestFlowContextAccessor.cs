using Aboitiz.Power.MobileAp.Core.Data.Diagnostics;
using Gluonics.Core.IoC;

namespace Aboitiz.Power.MobileAp.Core.Services.Diagnostics;

[AutoRegisterSingleton<IRequestFlowContextAccessor>]
internal sealed class RequestFlowContextAccessor
    : IRequestFlowContextAccessor
{
#nullable enable
    private static readonly AsyncLocal<string?>
        CurrentOperationIdHolder = new();

    public string? CurrentOperationId
    {
        get => CurrentOperationIdHolder.Value;
        set => CurrentOperationIdHolder.Value = value;
    }
}
