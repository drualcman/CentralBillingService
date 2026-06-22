namespace CentralBillingService.Application.DTOs;

public sealed record SendContactMessageCommand
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Message { get; init; }
}
