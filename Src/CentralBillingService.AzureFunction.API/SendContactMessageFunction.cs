namespace CentralBillingService.AzureFunction.API;

public sealed class SendContactMessageFunction
{
    private readonly SendContactMessageUseCase _useCase;
    private readonly ILogger<SendContactMessageFunction> _logger;

    public SendContactMessageFunction(
        SendContactMessageUseCase useCase,
        ILogger<SendContactMessageFunction> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/contact
    /// Receives a message from the public contact form and forwards it by email.
    /// The form itself is protected against bots with a client-side captcha, so this
    /// endpoint stays anonymous and only validates the payload.
    /// </summary>
    [Function(nameof(SendContactMessageFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        SendContactMessageCommand command;

        try
        {
            var request = await HttpRequestBodyHelper.GetRequestedModel<ContactMessageRequest>(req, cancellationToken);

            command = new SendContactMessageCommand
            {
                Name = request.Name ?? string.Empty,
                Email = request.Email,
                Message = request.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid request body for contact message.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.BadRequest, "Invalid request body.", ex.Message);
        }

        try
        {
            var result = await _useCase.ExecuteAsync(command, cancellationToken);

            var response = req.CreateResponse(
                result.Success ? HttpStatusCode.OK : HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(result, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending contact message.");
            return await req.CreateProblemResponseAsync(
                HttpStatusCode.InternalServerError, "Unexpected error.", "An internal error occurred.");
        }
    }
}
