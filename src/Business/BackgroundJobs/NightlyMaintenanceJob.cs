using Business.Abstract;

namespace Business.BackgroundJobs;

/// <summary>docs/MIMARI.md · K-08/§7: Hangfire recurring job — Program.cs'te "gecelik-bakim" adıyla kaydedilir.</summary>
public sealed class NightlyMaintenanceJob(IMaintenanceService maintenanceService)
{
    public async Task RunAsync() =>
        await maintenanceService.RunNightlyMaintenanceAsync().ConfigureAwait(false);
}
