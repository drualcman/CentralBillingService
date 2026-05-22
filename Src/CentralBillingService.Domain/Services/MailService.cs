namespace CentralBillingService.Domain.Services;

internal class MailService(IServiceScopeFactory ServiceScopeFactory) : IMailService
{
    public async ValueTask Send(string name, string email, string subject, string message, CancellationToken token)
    {
        // Validación básica de parámetros obligatorios
        if (ValidateRequest(email, subject, message))
        {
            var mail = EmailBuilder.Build(name, email, subject, message);
            await Send(mail, token);
        }
    }
    public ValueTask Send(Email email, CancellationToken token)
    {
        // Ejecutamos el envío en segundo plano para no bloquear el hilo del caller
        _ = Task.Run(async () =>
        {
            await using var scope = ServiceScopeFactory.CreateAsyncScope();
            var emailOptions = scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>();
            var iso9001 = scope.ServiceProvider.GetRequiredService<IIso9001>();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            var internalToken = linkedCts.Token;

            using var client = CreateHttpClient(emailOptions.Value.Url);

            try
            {
                await SendEmailWithContactPersonalizationAsync(client, email, internalToken);
            }
            catch (Exception ex)
            {
                await iso9001.Error(email.Recipients[0].Adressee, this, ex);
                throw;
            }
        }, token);
        return ValueTask.CompletedTask;
    }

    private bool ValidateRequest(string email, string subject, string message) =>
        !string.IsNullOrWhiteSpace(email) &&
                    !string.IsNullOrWhiteSpace(subject) &&
                    !string.IsNullOrWhiteSpace(message);

    private HttpClient CreateHttpClient(string baseUrl) => new HttpClient { BaseAddress = new Uri(baseUrl) } ?? throw new ArgumentNullException(nameof(baseUrl));

    private string FormatText(string text, Addressee recipient) => string.IsNullOrEmpty(text) ? "" : text.Replace("[contact]", recipient.DisplayName);

    private async Task SendEmailWithContactPersonalizationAsync(HttpClient client, Email originalMail, CancellationToken token)
    {
        var responseHandler = new SendResponseHandler($"{nameof(MailService)}.{nameof(Send)}");

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = 5,
            CancellationToken = token
        };

        await Parallel.ForEachAsync(originalMail.Recipients, parallelOptions, async (recipient, ct) =>
        {
            var personalizedBody = FormatText(originalMail.Content, recipient);
            var templatedBody = MailTemplates.GetEmailTemplate(personalizedBody);
            var personalizedSubject = FormatText(originalMail.Subject, recipient);

            var mailToSend = new Email(personalizedSubject, templatedBody, attachments: originalMail.Attachments);
            mailToSend.Recipients.Add(recipient);

            var response = await SendSingleEmailAsync(client, mailToSend, token);
            responseHandler.Handle(response);
        });

        responseHandler.ThrowProblemDetailsExceptionIfSomeFalseResponse();
    }

    private async Task<HttpResponseMessage> SendSingleEmailAsync(HttpClient client, Email email, CancellationToken token) =>
        await client.PostAsJsonAsync("send-mail", email, token);
}