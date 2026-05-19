using DigitalDoor.Reporting.Entities.Interfaces;
using DigitalDoor.Reporting.Entities.ViewModels;

namespace CentralBillingService.VerifyUI.Services;

internal class FakePdfGenerator : IReportAsBytes
{
    public async Task<byte[]> GenerateReport(ReportViewModel reportModel)
    {
        byte[] data = [];
        await Task.Delay(1);
        return data;
    }
}
