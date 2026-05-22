namespace CentralBillingService.Domain.ValueObjects;

internal struct SendResponse
{
    public bool Result { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }

    public SendResponse()
    {

    }

    public SendResponse(bool result, string title, string message)
    {
        Result = result;
        Title = title;
        Message = message;
    }
}
