using Business.Abstract;

namespace Business.BackgroundJobs;

/// <summary>docs/MIMARI.md · Y-47: yalnızca id alır, aspect'li Business arayüzünü çağırır.</summary>
public sealed class ReportGenerationJob(IReportGenerationService reportGenerationService)
{
    public async Task GenerateAsync(int reportRequestId) =>
        await reportGenerationService.GenerateAsync(reportRequestId).ConfigureAwait(false);
}
