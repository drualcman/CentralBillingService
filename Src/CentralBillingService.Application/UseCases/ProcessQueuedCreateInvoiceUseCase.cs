namespace CentralBillingService.Application.UseCases;

/// <summary>
/// Handles invoice creation triggered from a queue message.
///
/// Flow:
///   1. Delegates to CreateInvoiceUseCase (unchanged — same logic as HTTP trigger)
///   2. After success, dispatches the result to whichever outputs are configured
///      for this BillingSource (output queue and/or HTTP callback).
///
/// The HTTP trigger calls CreateInvoiceUseCase directly and returns the result.
/// This use case is only invoked by the queue trigger function.
/// Failures in result dispatch do NOT roll back the invoice.
/// </summary>
public sealed class ProcessQueuedCreateInvoiceUseCase
{
    private readonly ICreateInvoiceUseCase _createUseCase;
    private readonly BillingSourceRegistry _registry;
    private readonly IInvoiceResultQueuePublisher _queuePublisher;
    private readonly IInvoiceResultCallbackNotifier _callbackNotifier;
    private readonly IIso9001 _iso9001;

    public ProcessQueuedCreateInvoiceUseCase(
        ICreateInvoiceUseCase createUseCase,
        BillingSourceRegistry registry,
        IInvoiceResultQueuePublisher queuePublisher,
        IInvoiceResultCallbackNotifier callbackNotifier,
        IIso9001 iso9001)
    {
        _createUseCase = createUseCase;
        _registry = registry;
        _queuePublisher = queuePublisher;
        _callbackNotifier = callbackNotifier;
        _iso9001 = iso9001;
    }

    public async Task<InvoiceResult> ExecuteAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _createUseCase.ExecuteAsync(command, cancellationToken);

        // Registry lookup succeeds here because CreateInvoiceUseCase already validated it.
        var config = _registry.GetConfig(command.BillingSource, command.Secret);

        if (config.ResultQueue is not null)
            await PublishToQueueSafelyAsync(result, config.ResultQueue, cancellationToken);

        if (config.Callback is not null)
            await NotifyCallbackSafelyAsync(result, config.Callback, cancellationToken);

        return result;
    }

    private async Task PublishToQueueSafelyAsync(
        InvoiceResult result, ResultQueueConfig config, CancellationToken ct)
    {
        try
        {
            await _queuePublisher.PublishAsync(result, config, ct);
        }
        catch (Exception ex)
        {
            await _iso9001.Error(result.InvoiceNumber, this, ex);
        }
    }

    private async Task NotifyCallbackSafelyAsync(
        InvoiceResult result, CallbackConfig config, CancellationToken ct)
    {
        try
        {
            await _callbackNotifier.NotifyAsync(result, config, ct);
        }
        catch (Exception ex)
        {
            await _iso9001.Error(result.InvoiceNumber, this, ex);
        }
    }
}
