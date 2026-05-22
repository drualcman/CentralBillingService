namespace CentralBillingService.Domain.Handlers;

internal class SendResponseHandler
{
    ConcurrentBag<SendResponse> Responses = new();
    readonly string Instance;

    public SendResponseHandler(string instance) => Instance = instance;

    internal void Handle(HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();
            Responses.Add(new SendResponse(true, $"{(int)response.StatusCode} {response.StatusCode}", response.ReasonPhrase));
            response.Dispose();
        }
        catch (Exception ex)
        {
            Handle(ex);
        }
    }

    void Handle(Exception exception)
    {
        Responses.Add(new SendResponse(false, exception.GetType().Name, exception.Message));
    }

    internal void ThrowProblemDetailsExceptionIfSomeFalseResponse()
    {
        IEnumerable<SendResponse> erros = Responses.Where(f => f.Result == false);
        if (erros is not null && erros.Any())
        {
            StringBuilder stringBuilder = new StringBuilder($"Instance: {Instance}");
            Dictionary<string, string> problems = new();
            foreach (SendResponse error in erros)
            {
                stringBuilder.AppendLine($"{error.Title}: {error.Message}");
            }
            throw new Exception(stringBuilder.ToString());
        }
    }
}

