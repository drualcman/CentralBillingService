namespace CentralBillingService.Tests.Unit.Application.UseCases;

public class SendContactMessageUseCaseTests
{
    private readonly IMailService _mailService;
    private readonly SendContactMessageUseCase _useCase;

    private const string OwnerMailbox = "owner@community-mall.com";

    public SendContactMessageUseCaseTests()
    {
        _mailService = Substitute.For<IMailService>();
        var options = Options.Create(new EmailOptions { ContactRecipient = OwnerMailbox });
        _useCase = new SendContactMessageUseCase(_mailService, options, Substitute.For<IIso9001>());
    }

    private static SendContactMessageCommand Command(
        string name = "Jane Doe",
        string email = "jane@example.com",
        string message = "I have a question about my invoice.") =>
        new() { Name = name, Email = email, Message = message };

    [Fact]
    public async Task ExecuteAsync_valid_message_sends_to_owner_and_visitor()
    {
        var captured = new List<Email>();
        _mailService
            .When(m => m.Send(Arg.Any<Email>(), Arg.Any<CancellationToken>()))
            .Do(call => captured.Add(call.Arg<Email>()));

        var result = await _useCase.ExecuteAsync(Command());

        Assert.True(result.Success);
        Assert.Equal(2, captured.Count);
        Assert.Contains(captured, e => e.Recipients.Any(r => r.Adressee == OwnerMailbox));
        Assert.Contains(captured, e => e.Recipients.Any(r => r.Adressee == "jane@example.com"));
    }

    [Fact]
    public async Task ExecuteAsync_invalid_email_returns_invalid_and_sends_nothing()
    {
        var result = await _useCase.ExecuteAsync(Command(email: "not-an-email"));

        Assert.False(result.Success);
        await _mailService.DidNotReceive().Send(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_empty_message_returns_invalid_and_sends_nothing()
    {
        var result = await _useCase.ExecuteAsync(Command(message: "   "));

        Assert.False(result.Success);
        await _mailService.DidNotReceive().Send(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_missing_name_falls_back_to_email()
    {
        var captured = new List<Email>();
        _mailService
            .When(m => m.Send(Arg.Any<Email>(), Arg.Any<CancellationToken>()))
            .Do(call => captured.Add(call.Arg<Email>()));

        var result = await _useCase.ExecuteAsync(Command(name: "  "));

        Assert.True(result.Success);
        var confirmation = captured.Single(e => e.Recipients.Any(r => r.Adressee == "jane@example.com"));
        Assert.Equal("jane@example.com", confirmation.Recipients.Single().DisplayName);
    }
}
