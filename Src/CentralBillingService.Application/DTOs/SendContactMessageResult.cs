namespace CentralBillingService.Application.DTOs;

public sealed record SendContactMessageResult
{
    public bool Success { get; init; }
    public string Message { get; init; }

    private SendContactMessageResult(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static SendContactMessageResult Sent() =>
        new(true, "Your message has been sent. We will get back to you as soon as possible.");

    public static SendContactMessageResult Invalid(string reason) =>
        new(false, reason);
}
