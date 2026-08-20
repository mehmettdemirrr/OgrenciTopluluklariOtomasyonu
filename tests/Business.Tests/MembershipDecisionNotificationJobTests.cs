using Business.Abstract;
using Business.BackgroundJobs;
using Core.DataAccess;
using Core.Utilities.Email;
using Entities;
using Entities.Enums;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/MIMARI.md · Y-47: bulunamama/hâlâ Pending olma durumunda sessizce çıkar — idempotentlik.</summary>
public class MembershipDecisionNotificationJobTests
{
    private readonly Mock<IEntityRepository<MembershipApplication>> _applicationRepository = new();
    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IIdentityGateway> _identityGateway = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly MembershipDecisionNotificationJob _sut;

    public MembershipDecisionNotificationJobTests()
    {
        _sut = new MembershipDecisionNotificationJob(
            _applicationRepository.Object,
            _clubRepository.Object,
            _studentRepository.Object,
            _identityGateway.Object,
            _emailSender.Object);
    }

    [Fact(DisplayName = "Onaylanmış başvuru için öğrenciye e-posta gönderilir")]
    public async Task ApprovedApplication_SendsEmail()
    {
        var application = new MembershipApplication { Id = 1, ClubId = 1, StudentId = 1, AcademicTermId = 1, Status = ApplicationStatus.Approved, AppliedAtUtc = DateTime.UtcNow };
        var student = new Student { Id = 1, ApplicationUserId = 50, StudentNumber = "20260001", DepartmentId = 1, EnrollmentYear = 2026 };
        var studentUser = new ApplicationUser { Id = 50, Email = "ogrenci@test.local", UserName = "ogrenci@test.local" };
        var club = new Club { Id = 1, Name = "Test Kulübü", AdvisorId = 1, IsActive = true, CreatedAtUtc = DateTime.UtcNow };

        _applicationRepository.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);
        _studentRepository.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(student);
        _identityGateway.Setup(g => g.FindByIdAsync(50)).ReturnsAsync(studentUser);
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(club);

        await _sut.SendAsync(1);

        _emailSender.Verify(
            e => e.SendAsync("ogrenci@test.local", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "Hâlâ Pending durumundaki (henüz karar verilmemiş) başvuru için e-posta gönderilmez")]
    public async Task PendingApplication_DoesNotSendEmail()
    {
        var application = new MembershipApplication { Id = 1, ClubId = 1, StudentId = 1, AcademicTermId = 1, Status = ApplicationStatus.Pending, AppliedAtUtc = DateTime.UtcNow };
        _applicationRepository.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(application);

        await _sut.SendAsync(1);

        _emailSender.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "Bulunamayan başvuru için e-posta gönderilmez (Hangfire retry güvenli)")]
    public async Task NotFoundApplication_DoesNotThrow_DoesNotSendEmail()
    {
        _applicationRepository
            .Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<MembershipApplication, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MembershipApplication?)null);

        await _sut.SendAsync(999);

        _emailSender.Verify(
            e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
