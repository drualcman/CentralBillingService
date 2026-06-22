using CentralBillingService.Domain.Options;
using Microsoft.Extensions.Options;

namespace CentralBillingService.Application.UseCases;

public sealed class SendContactMessageUseCase
{
    private readonly IMailService _mailService;
    private readonly EmailOptions _emailOptions;
    private readonly IIso9001 _iso9001;

    public SendContactMessageUseCase(
        IMailService mailService,
        IOptions<EmailOptions> emailOptions,
        IIso9001 iso9001)
    {
        _mailService = mailService;
        _emailOptions = emailOptions.Value;
        _iso9001 = iso9001;
    }

    public async Task<SendContactMessageResult> ExecuteAsync(
        SendContactMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        var name = command.Name?.Trim() ?? string.Empty;
        var email = command.Email?.Trim() ?? string.Empty;
        var message = command.Message?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            return SendContactMessageResult.Invalid("A valid email address is required.");

        if (string.IsNullOrWhiteSpace(message))
            return SendContactMessageResult.Invalid("The message cannot be empty.");

        if (string.IsNullOrWhiteSpace(name))
            name = email;

        await _iso9001.Register(email, this, "Contact message received from verification portal", command);

        // 1) Notification to the mailbox owner with the visitor's details.
        var notification = new Email(
            subject: $"Nuevo mensaje de contacto de {name}",
            content: BuildNotificationBody(name, email, message));
        notification.AddAddressee(_emailOptions.ContactRecipient);
        await _mailService.Send(notification, cancellationToken);

        // 2) Confirmation back to the visitor.
        var confirmation = new Email(
            subject: "Hemos recibido tu mensaje · We have received your message",
            content: BuildConfirmationBody());
        confirmation.AddAddressee(new Addressee(email, name));
        await _mailService.Send(confirmation, cancellationToken);

        return SendContactMessageResult.Sent();
    }

    private static string BuildNotificationBody(string name, string email, string message) => $@"
        <p>Has recibido un nuevo mensaje desde el formulario de contacto del portal de verificación de facturas.</p>
        <table style=""border-collapse:collapse;"">
            <tr><td style=""padding:4px 12px 4px 0;""><strong>Nombre</strong></td><td>{Encode(name)}</td></tr>
            <tr><td style=""padding:4px 12px 4px 0;""><strong>Email</strong></td><td><a href=""mailto:{Encode(email)}"">{Encode(email)}</a></td></tr>
        </table>
        <p style=""margin-top:16px;""><strong>Mensaje:</strong></p>
        <p style=""white-space:pre-wrap;"">{Encode(message)}</p>";

    private static string BuildConfirmationBody() => $@"
        <p>Hola <strong>[contact]</strong>,</p>
        <p>Hemos recibido tu mensaje y te responderemos lo antes posible. Gracias por ponerte en contacto con nosotros.</p>
        <hr style=""border:none;border-top:1px solid #e0e0e0;margin:20px 0;"" />
        <p>Hello <strong>[contact]</strong>,</p>
        <p>We have received your message and will get back to you as soon as possible. Thank you for reaching out.</p>";

    private static string Encode(string value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

    private static bool IsValidEmail(string email)
    {
        try
        {
            var address = new System.Net.Mail.MailAddress(email);
            return address.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
