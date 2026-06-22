namespace CentralBillingService.Domain.Options;

public class EmailOptions
{
    public const string SectionKey = nameof(EmailOptions);
    public string Url { get; set; } = string.Empty;
    public int CompanyId { get; set; } = 3;

    /// <summary>
    /// Mailbox that receives the messages sent from the public contact form.
    /// </summary>
    public string ContactRecipient { get; set; } = "info@community-mall.com";
}
