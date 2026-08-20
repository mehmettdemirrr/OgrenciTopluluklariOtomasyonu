using Business.Abstract;
using Business.BackgroundJobs;
using Business.Constants;
using Business.DTOs.Memberships;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;
using Hangfire;

namespace Business.Concrete;

public sealed class MembershipApplicationManager(
    IEntityRepository<MembershipApplication> membershipApplicationRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IBackgroundJobClient backgroundJobClient) : IMembershipApplicationService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IResult> ApplyAsync(ApplyForMembershipRequestDto request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Unauthorized(Messages.NotAStudent);
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Result.Forbidden(Messages.NotAStudent);
        }

        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Result.NotFound(Messages.NoCurrentAcademicTerm);
        }

        var club = await clubRepository.GetAsync(c => c.Id == request.ClubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        if (!club.IsActive)
        {
            return Result.Conflict(Messages.ClubNotActive);
        }

        var existingMembership = await clubMembershipRepository
            .GetAsync(m => m.ClubId == club.Id && m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
            .ConfigureAwait(false);
        if (existingMembership is not null)
        {
            return Result.Conflict(Messages.AlreadyClubMember);
        }

        var existingApplication = await membershipApplicationRepository
            .GetAsync(
                a => a.ClubId == club.Id && a.StudentId == student.Id && a.AcademicTermId == term.Id && a.Status == ApplicationStatus.Pending,
                cancellationToken)
            .ConfigureAwait(false);
        if (existingApplication is not null)
        {
            return Result.Conflict(Messages.DuplicatePendingApplication);
        }

        var application = new MembershipApplication
        {
            ClubId = club.Id,
            StudentId = student.Id,
            AcademicTermId = term.Id,
            Status = ApplicationStatus.Pending,
            AppliedAtUtc = clock.UtcNow,
        };

        await membershipApplicationRepository.AddAsync(application, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ApplicationSubmitted);
    }

    public async Task<IDataResult<PagedResult<MembershipApplicationListItemDto>>> GetPendingForAdvisorAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);

        var advisor = currentUser.UserId is { } userId
            ? await academicStaffRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false)
            : null;

        if (advisor is null)
        {
            return DataResult<PagedResult<MembershipApplicationListItemDto>>.Success(
                new PagedResult<MembershipApplicationListItemDto>([], 0, pageIndex, clampedPageSize));
        }

        var clubIds = (await clubRepository.GetListAsync(c => c.AdvisorId == advisor.Id, cancellationToken).ConfigureAwait(false))
            .Select(c => c.Id)
            .ToList();

        var paged = await membershipApplicationRepository
            .GetListPagedAsync(
                pageIndex, clampedPageSize, a => clubIds.Contains(a.ClubId) && a.Status == ApplicationStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        var clubNames = (await clubRepository.GetListAsync(c => clubIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        var studentIds = paged.Items.Select(a => a.StudentId).Distinct().ToList();
        var studentNumbers = (await studentRepository.GetListAsync(s => studentIds.Contains(s.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(s => s.Id, s => s.StudentNumber);

        var items = paged.Items.Select(a => new MembershipApplicationListItemDto
        {
            Id = a.Id,
            ClubId = a.ClubId,
            ClubName = clubNames.GetValueOrDefault(a.ClubId, string.Empty),
            StudentId = a.StudentId,
            StudentNumber = studentNumbers.GetValueOrDefault(a.StudentId, string.Empty),
            Status = a.Status,
            AppliedAtUtc = a.AppliedAtUtc,
        }).ToList();

        var result = new PagedResult<MembershipApplicationListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<MembershipApplicationListItemDto>>.Success(result);
    }

    public async Task<IResult> ReviewAsync(int applicationId, ReviewMembershipApplicationRequestDto request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Forbidden(Messages.NotClubAdvisor);
        }

        var application = await membershipApplicationRepository.GetAsync(a => a.Id == applicationId, cancellationToken).ConfigureAwait(false);
        if (application is null)
        {
            return Result.NotFound(Messages.MembershipApplicationNotFound);
        }

        if (application.Status != ApplicationStatus.Pending)
        {
            return Result.Conflict(Messages.ApplicationAlreadyReviewed);
        }

        var club = await clubRepository.GetAsync(c => c.Id == application.ClubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Result.NotFound(Messages.ClubNotFound);
        }

        // Y-23: izin claim'i (memberships.write) yeterli değil — yalnızca bu kulübün danışmanı onaylayabilir.
        var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is null || advisor.ApplicationUserId != userId)
        {
            return Result.Forbidden(Messages.NotClubAdvisor);
        }

        var now = clock.UtcNow;

        // Y-46/Y-06: [TransactionAspect] kasıtlı olarak kullanılmaz — commit'ten SONRA Hangfire'a
        // kuyruğa ekleme yapılabilmesi için transaction burada elle yönetilir.
        var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            try
            {
                application.Status = request.Status;
                application.ReviewedAtUtc = now;
                application.ReviewedByUserId = userId;
                membershipApplicationRepository.Update(application);

                if (request.Status == ApplicationStatus.Approved)
                {
                    var membership = new ClubMembership
                    {
                        ClubId = application.ClubId,
                        StudentId = application.StudentId,
                        AcademicTermId = application.AcademicTermId,
                        ClubRole = ClubRole.Member,
                        JoinedAtUtc = now,
                    };
                    await clubMembershipRepository.AddAsync(membership, cancellationToken).ConfigureAwait(false);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        // Y-46: kuyruğa ekleme yalnızca commit'ten sonra — worker henüz var olmayan kaydı aramasın.
        backgroundJobClient.Enqueue<MembershipDecisionNotificationJob>(job => job.SendAsync(application.Id));

        return Result.Success(request.Status == ApplicationStatus.Approved ? Messages.ApplicationApproved : Messages.ApplicationRejected);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
