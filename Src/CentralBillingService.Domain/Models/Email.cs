namespace CentralBillingService.Domain.Models;

public class Email
{
    public string Subject { get; set; }
    public int CompanyId { get; set; } = 5;
    public List<Addressee> Recipients { get; set; }
    public string Content { get; set; }
    public List<Attachment> Attachments { get; set; }

    public Email(string subject = "", string content = "", List<Addressee> addresseeList = null, List<Attachment> attachments = null)
    {
        Subject = $"[Shot Up Albums] {subject.Replace("[Shot Up Albums]", "").Trim()}";
        Content = content;
        Attachments = attachments is null ? [] : attachments;
        Recipients = [];
        if (addresseeList is not null)
            SetAddresseeList(addresseeList);
    }

    public void SetAddresseeList(List<Addressee> addresseeList) => Recipients = addresseeList;
    public void AddAddressee(string email)
    {
        if (!string.IsNullOrEmpty(email))
        {
            if (email.Contains(";"))
            {
                string[] mails = email.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (string mail in mails)
                {
                    AddAddressee(new Addressee(mail, CheckHasEmailAddress(mail)));
                }
            }
            else
            {
                AddAddressee(new Addressee(email, CheckHasEmailAddress(email)));
            }
        }
    }
    private string CheckHasEmailAddress(string mail)
    {
        if (mail.IndexOf("@") > 0)
            return mail.Substring(0, mail.IndexOf("@"));
        else
            return mail;

    }

    public void AddAddressee(Addressee addressee) => Recipients.Add(addressee);
    public void RemoveAddressee(Addressee addressee)
    {
        if (Recipients.Any(p => p == addressee))
            Recipients.Remove(addressee);
    }

    private string Addressees()
    {
        StringBuilder addressees = new StringBuilder();
        foreach (Addressee addressee in Recipients)
        {
            if (string.IsNullOrEmpty(addressee.DisplayName))
            {
                addressees.Append(addressee.Adressee);
            }
            else
            {
                addressees.Append(addressee.DisplayName);
                addressees.Append(" [");
                addressees.Append(addressee.Adressee);
                addressees.Append("]");
            }
            addressees.Append(",");
        }
        addressees.Remove(addressees.Length - 1, 1);
        return addressees.ToString();
    }

    public override string ToString() =>
        $"{Content} to: {Addressees()}";

    public void AddAttach(Attachment attachment) => Attachments.Add(attachment);
    public void RemovePhone(Attachment attachment)
    {
        if (Attachments.Any(p => p == attachment))
            Attachments.Remove(attachment);
    }
}
