using Business.Abstract;
using Business.DTOs.Reports;
using Core.DataAccess;
using DataAccess.Seed;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

/// <summary>
/// docs/MIMARI.md · Y-51: kapsam token'dan değil, her çağrıda DB'den çözülür (K-01'in "izinler her
/// refresh'te yeniden çözülür" seam'iyle aynı fikir).
/// </summary>
public sealed class ReportScopeResolver(
    IIdentityGateway identityGateway,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<AcademicTerm> academicTermRepository) : IReportScopeResolver
{
    public async Task<ReportScope> ResolveAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await identityGateway.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return ReportScope.None;
        }

        var permissions = await identityGateway.GetPermissionsAsync(user).ConfigureAwait(false);
        if (permissions.Contains(IdentitySeedData.Permissions.ReportsReadAll))
        {
            return new ReportScope(true, []);
        }

        if (!permissions.Contains(IdentitySeedData.Permissions.ReportsRead))
        {
            return ReportScope.None;
        }

        var clubIds = new HashSet<int>();

        var advisor = await academicStaffRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (advisor is not null)
        {
            var advisedClubs = await clubRepository.GetListAsync(c => c.AdvisorId == advisor.Id, cancellationToken).ConfigureAwait(false);
            foreach (var club in advisedClubs)
            {
                clubIds.Add(club.Id);
            }
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (student is not null && term is not null)
        {
            var officerMemberships = await clubMembershipRepository
                .GetListAsync(
                    m => m.StudentId == student.Id
                        && m.AcademicTermId == term.Id
                        && (m.ClubRole == ClubRole.Officer || m.ClubRole == ClubRole.President),
                    cancellationToken)
                .ConfigureAwait(false);

            foreach (var membership in officerMemberships)
            {
                clubIds.Add(membership.ClubId);
            }
        }

        return new ReportScope(false, clubIds);
    }
}
