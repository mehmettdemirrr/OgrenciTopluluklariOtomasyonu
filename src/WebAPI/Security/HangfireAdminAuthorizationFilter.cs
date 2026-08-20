using Core.Utilities.Security;
using Hangfire.Dashboard;

namespace WebAPI.Security;

/// <summary>
/// docs/MIMARI.md · A-29/K-14: panel yalnızca yönetici izniyle — Hangfire V1'in tek izleme aracı.
/// Y-01/Y-05: WebAPI, DataAccess'teki izin kataloğuna (IdentitySeedData) bağımlı olamaz — bu yüzden
/// izin kodu burada da DiagnosticsController'daki [SecuredOperation("diagnostics.protected")] ile
/// aynı desenle literal string olarak taşınır, DataAccess.Seed.IdentitySeedData'dan import edilmez.
/// </summary>
public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    private const string HangfireDashboardPermission = "hangfire.dashboard";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.FindAll(CurrentUserClaimTypes.Permission)
                .Any(c => c.Value == HangfireDashboardPermission);
    }
}
