namespace CentralBillingService.AzureFunction.API.Requests;

internal sealed class ContactMessageRequest
{
    public string? Name { get; init; }
    public required string Email { get; init; }
    public required string Message { get; init; }
}
