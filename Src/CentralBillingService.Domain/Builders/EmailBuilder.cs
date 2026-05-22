namespace CentralBillingService.Domain.Builders;

public static class EmailBuilder
{
    public static Email Build(string name, string email, string subject, string message)
    {
        var mail = new Email(subject, message);
        mail.AddAddressee(new Addressee(email, name));
        return mail;
    }
}
