using System.Linq.Expressions;
using Business.Abstract;
using Business.Concrete;
using Business.DTOs.Auth;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Entities;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>docs/PLAN-V2.md · A-20/Faz 11: her iş kuralı için bir kabul + bir ret.</summary>
public class AccountManagerTests
{
    private readonly Mock<IAccountGateway> _accountGateway = new();
    private readonly Mock<IIdentityGateway> _identityGateway = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<Department>> _departmentRepository = new();
    private readonly Mock<IEntityRepository<Faculty>> _facultyRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IBackgroundJobClient> _backgroundJobClient = new();
    private readonly AccountManager _sut;

    public AccountManagerTests()
    {
        var transaction = new Mock<ITransaction>();
        transaction.Setup(t => t.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        transaction.Setup(t => t.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);

        _sut = new AccountManager(
            _accountGateway.Object,
            _identityGateway.Object,
            _studentRepository.Object,
            _departmentRepository.Object,
            _facultyRepository.Object,
            _unitOfWork.Object,
            _currentUser.Object,
            _backgroundJobClient.Object);
    }

    private static RegisterRequestDto ValidRegisterRequest() => new()
    {
        Email = "new-student@test.local", Password = "Str0ng!Pass", StudentNumber = "20260099", DepartmentId = 1, EnrollmentYear = 2026,
    };

    [Fact(DisplayName = "Register: geçerli koşullarda kullanıcı+öğrenci oluşturulur ve doğrulama işi kuyruğa eklenir")]
    public async Task RegisterAsync_ValidRequest_CreatesUserAndEnqueuesConfirmationJob()
    {
        _accountGateway.Setup(g => g.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);
        _departmentRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Department { Id = 1, Name = "Bilgisayar Mühendisliği", FacultyId = 1 });
        _accountGateway.Setup(g => g.CreateUserAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .Callback<ApplicationUser, string>((u, _) => u.Id = 42)
            .ReturnsAsync(true);

        var result = await _sut.RegisterAsync(ValidRegisterRequest());

        Assert.True(result.IsSuccess);
        _studentRepository.Verify(r => r.AddAsync(It.IsAny<Student>(), It.IsAny<CancellationToken>()), Times.Once);
        _accountGateway.Verify(g => g.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Member"), Times.Once);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact(DisplayName = "Register: e-posta zaten kayıtlıysa reddedilir, kullanıcı oluşturulmaz")]
    public async Task RegisterAsync_EmailAlreadyRegistered_ReturnsConflict()
    {
        _accountGateway.Setup(g => g.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Id = 1, Email = "new-student@test.local" });

        var result = await _sut.RegisterAsync(ValidRegisterRequest());

        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        _accountGateway.Verify(g => g.CreateUserAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact(DisplayName = "ConfirmEmail: geçerli token ile hesap doğrulanır")]
    public async Task ConfirmEmailAsync_ValidToken_ReturnsSuccess()
    {
        var user = new ApplicationUser { Id = 1, Email = "x@test.local", EmailConfirmed = false };
        _accountGateway.Setup(g => g.FindByIdAsync(1)).ReturnsAsync(user);
        _accountGateway.Setup(g => g.ConfirmEmailAsync(user, "valid-token")).ReturnsAsync(true);

        var result = await _sut.ConfirmEmailAsync(new ConfirmEmailRequestDto { UserId = 1, Token = "valid-token" });

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "ConfirmEmail: eşzamanlı ikinci istek (RowVersion çakışması) hata değil başarı sayılır")]
    public async Task ConfirmEmailAsync_ConcurrentConfirmation_ReturnsSuccessInsteadOfThrowing()
    {
        var user = new ApplicationUser { Id = 1, Email = "x@test.local", EmailConfirmed = false };
        _accountGateway.Setup(g => g.FindByIdAsync(1)).ReturnsAsync(user);
        _accountGateway.Setup(g => g.ConfirmEmailAsync(user, "valid-token"))
            .ThrowsAsync(new ConcurrencyConflictException("conflict", new Exception()));

        var result = await _sut.ConfirmEmailAsync(new ConfirmEmailRequestDto { UserId = 1, Token = "valid-token" });

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "ConfirmEmail: geçersiz/süresi dolmuş token reddedilir")]
    public async Task ConfirmEmailAsync_InvalidToken_ReturnsConflict()
    {
        var user = new ApplicationUser { Id = 1, Email = "x@test.local", EmailConfirmed = false };
        _accountGateway.Setup(g => g.FindByIdAsync(1)).ReturnsAsync(user);
        _accountGateway.Setup(g => g.ConfirmEmailAsync(user, "bad-token")).ReturnsAsync(false);

        var result = await _sut.ConfirmEmailAsync(new ConfirmEmailRequestDto { UserId = 1, Token = "bad-token" });

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "ForgotPassword: kayıtlı e-posta için iş kuyruğa eklenir")]
    public async Task ForgotPasswordAsync_KnownEmail_EnqueuesJob()
    {
        var user = new ApplicationUser { Id = 7, Email = "known@test.local" };
        _accountGateway.Setup(g => g.FindByEmailAsync("known@test.local")).ReturnsAsync(user);

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "known@test.local" });

        Assert.True(result.IsSuccess);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Once);
    }

    [Fact(DisplayName = "ForgotPassword: Y-55 — kayıtlı olmayan e-posta AYNI başarı cevabını döner, iş kuyruğa eklenmez")]
    public async Task ForgotPasswordAsync_UnknownEmail_ReturnsSameSuccessWithoutEnqueuing()
    {
        _accountGateway.Setup(g => g.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser?)null);

        var result = await _sut.ForgotPasswordAsync(new ForgotPasswordRequestDto { Email = "unknown@test.local" });

        Assert.True(result.IsSuccess);
        _backgroundJobClient.Verify(c => c.Create(It.IsAny<Job>(), It.IsAny<IState>()), Times.Never);
    }

    [Fact(DisplayName = "ChangePassword: doğru mevcut parola ile değiştirilir")]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_ReturnsSuccess()
    {
        var user = new ApplicationUser { Id = 5, Email = "x@test.local" };
        _currentUser.Setup(c => c.UserId).Returns(5);
        _accountGateway.Setup(g => g.FindByIdAsync(5)).ReturnsAsync(user);
        _accountGateway.Setup(g => g.ChangePasswordAsync(user, "old", "New1!aaaa")).ReturnsAsync(true);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordRequestDto { CurrentPassword = "old", NewPassword = "New1!aaaa" });

        Assert.True(result.IsSuccess);
    }

    [Fact(DisplayName = "ChangePassword: yanlış mevcut parola reddedilir")]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsConflict()
    {
        var user = new ApplicationUser { Id = 5, Email = "x@test.local" };
        _currentUser.Setup(c => c.UserId).Returns(5);
        _accountGateway.Setup(g => g.FindByIdAsync(5)).ReturnsAsync(user);
        _accountGateway.Setup(g => g.ChangePasswordAsync(user, "wrong", "New1!aaaa")).ReturnsAsync(false);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordRequestDto { CurrentPassword = "wrong", NewPassword = "New1!aaaa" });

        Assert.False(result.IsSuccess);
    }
}
