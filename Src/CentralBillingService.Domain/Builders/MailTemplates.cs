namespace CentralBillingService.Domain.Builders;

internal static class MailTemplates
{
    public static string GetEmailTemplate(string body)
    {
        const string logoUrl = "https://drualcman.blob.core.windows.net/content/SergiLogo.png";
        int year = DateTime.UtcNow.Year;

        string result =
        $$"""
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="UTF-8">
        </head>
        <body style="margin:0;padding:0;background-color:#ffffff;font-family:Arial,sans-serif;color:#2d3c45;">
            <table role="presentation" cellpadding="0" cellspacing="0" border="0" width="100%" style="border-collapse:collapse;table-layout:fixed;">
                <tr>
                    <td style="padding:20px;">
                        <table align="center" role="presentation" cellpadding="0" cellspacing="0" border="0" width="700" style="border-collapse:collapse;min-width:700px;background-color:#ffffff;border:1px solid #e0e0e0;border-radius:8px;box-shadow:0 2px 8px rgba(0,0,0,0.05);margin:0 auto;">
                            <tr>
                                <td style="text-align:center;padding:20px 0 0 0;background-color:#ffffff;">
                                    <img src="{{logoUrl}}" alt="CBS Logo" title="CBS Logo" style="width:192px;height:auto;display:block;margin:0 auto;border:0;" />
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:15px;color:#2d3c45;text-align:left;">
                                    {{body}}
                                </td>
                            </tr>
                            <tr>
                                <td style="padding:15px;text-align:center;background-color:#2d3c45;color:#ffffff;font-size:12px;">
                                    &copy; {{year}} Sergi Ortiz Gomez. All rights reserved.
                                </td>
                            </tr>
                        </table>
                    </td>
                </tr>
            </table>

        </body>
        </html>
        """;

        return result;
    }
}