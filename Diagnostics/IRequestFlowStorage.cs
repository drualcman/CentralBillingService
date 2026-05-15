namespace Aboitiz.Power.MobileAp.Core.Data.Diagnostics;

public interface IRequestFlowStorage
{
    Task AppendAsync(
        RequestFlowEntry entry,
        CancellationToken cancellationToken = default);
}
