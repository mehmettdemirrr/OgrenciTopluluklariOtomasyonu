using Business.Abstract;
using Business.Constants;
using Business.DTOs.Dashboard;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Dtos.Dashboard;
using Entities.Dtos.Reports;

namespace Business.Concrete;

/// <summary>docs/PLAN-V2.md · Faz 12 (K-25): kişisel + (varsa) yönetim kapsamı sayıları tek özette birleşir.</summary>
public sealed class DashboardManager(
    IDashboardDal dashboardDal,
    IReportDal reportDal,
    IReportScopeResolver scopeResolver,
    IEntityRepository<Student> studentRepository,
    ICurrentUser currentUser,
    IClock clock) : IDashboardService
{
    public async Task<IDataResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return DataResult<DashboardSummaryDto>.Forbidden(Messages.DashboardAccessDenied);
        }

        var now = clock.UtcNow;

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        var personal = student is not null
            ? await dashboardDal.GetPersonalStatsAsync(student.Id, now, cancellationToken).ConfigureAwait(false)
            : new PersonalDashboardStatsDto();

        var scope = await scopeResolver.ResolveAsync(userId, cancellationToken).ConfigureAwait(false);

        ManagementDashboardStatsDto? management = null;
        IReadOnlyCollection<TermSummaryRowDto> termTrend = [];
        if (scope.AllClubs || scope.ClubIds.Count > 0)
        {
            management = await dashboardDal
                .GetManagementStatsAsync(scope.AllClubs, scope.ClubIds, now, cancellationToken)
                .ConfigureAwait(false);
            termTrend = await reportDal
                .GetTermSummaryAsync(scope.AllClubs ? null : scope.ClubIds, cancellationToken)
                .ConfigureAwait(false);
        }

        return DataResult<DashboardSummaryDto>.Success(new DashboardSummaryDto
        {
            Personal = personal,
            Management = management,
            TermTrend = termTrend,
        });
    }
}
