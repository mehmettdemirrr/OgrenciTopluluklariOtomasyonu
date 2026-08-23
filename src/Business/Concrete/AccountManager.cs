using Business.Abstract;
using Business.BackgroundJobs;
using Business.Constants;
using Business.DTOs.Auth;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using DataAccess.Seed;
using Entities;
using Hangfire;

namespace Business.Concrete;

/// <summary>docs/PLAN-V2.md · Faz 11 (K-03/A-40): kayıt, e-posta doğrulama, şifre sıfırlama, profil.</summary>
public sealed class AccountManager(
    IAccountGateway accountGateway,
    IIdentityGateway identityGateway,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<Department> departmentRepository,
    IEntityRepository<Faculty> facultyRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IBackgroundJobClient backgroundJobClient) : IAccountService
{
    public async Task<IResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        if (await accountGateway.FindByEmailAsync(email).ConfigureAwait(false) is not null)
        {
            return Result.Conflict(Messages.EmailAlreadyRegistered);
        }

        var department = await departmentRepository.GetAsync(d => d.Id == request.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (department is null)
        {
            return Result.NotFound(Messages.DepartmentNotFound);
        }

        var user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = false };

        // Y-46: [TransactionAspect] kasıtlı olarak kullanılmaz — commit'ten SONRA Hangfire'a doğrulama
        // e-postası işi eklenebilmesi için transaction burada elle yönetilir (ReviewAsync precedent'i).
        // Not: UserManager.CreateAsync de AYNI scoped AppDbContext üzerinden SaveChanges çağırır, bu
        // yüzden aşağıdaki BeginTransactionAsync onu da kapsar — kullanıcı satırı ile Student profili
        // ya birlikte commit olur ya da birlikte geri alınır.
        var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            try
            {
                if (!await accountGateway.CreateUserAsync(user, request.Password).ConfigureAwait(false))
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return Result.ValidationError(Messages.WeakPassword);
                }

                await accountGateway.AddToRoleAsync(user, IdentitySeedData.MemberRoleName).ConfigureAwait(false);

                var student = new Student
                {
                    ApplicationUserId = user.Id,
                    StudentNumber = request.StudentNumber.Trim(),
                    DepartmentId = department.Id,
                    EnrollmentYear = request.EnrollmentYear,
                };
                await studentRepository.AddAsync(student, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        // Y-46: kuyruğa ekleme yalnızca commit'ten sonra — worker henüz var olmayan kullanıcıyı aramasın.
        backgroundJobClient.Enqueue<EmailConfirmationJob>(job => job.SendAsync(user.Id));

        return Result.Success(Messages.RegistrationSucceeded);
    }

    public async Task<IResult> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await accountGateway.FindByIdAsync(request.UserId).ConfigureAwait(false);
        if (user is null)
        {
            return Result.NotFound(Messages.UserNotFound);
        }

        if (user.EmailConfirmed)
        {
            return Result.Success(Messages.EmailConfirmedSuccessfully);
        }

        try
        {
            var confirmed = await accountGateway.ConfirmEmailAsync(user, request.Token).ConfigureAwait(false);
            return confirmed ? Result.Success(Messages.EmailConfirmedSuccessfully) : Result.Conflict(Messages.InvalidOrExpiredToken);
        }
        catch (ConcurrencyConflictException)
        {
            // Aynı bağlantıya eşzamanlı ikinci bir istek (ör. çift tıklama, sayfa iki sekmede açık) —
            // ApplicationUser.ConcurrencyStamp'ı bu isteğin okuduğu andan sonra başka bir istek zaten
            // değiştirdi. Doğrulama kavramsal olarak idempotent olduğu için bunu hata değil, muhtemel
            // "az önce başka bir istek zaten doğruladı" durumu sayıyoruz.
            return Result.Success(Messages.EmailConfirmedSuccessfully);
        }
    }

    public async Task<IResult> ResendConfirmationAsync(ResendConfirmationRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await accountGateway.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user is not null && !user.EmailConfirmed)
        {
            backgroundJobClient.Enqueue<EmailConfirmationJob>(job => job.SendAsync(user.Id));
        }

        // Y-55: e-posta kayıtlı olmasa da, zaten doğrulanmış olsa da AYNI cevap — enumeration sızıntısı yok.
        return Result.Success(Messages.ConfirmationResent);
    }

    public async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await accountGateway.FindByEmailAsync(request.Email.Trim()).ConfigureAwait(false);
        if (user is not null)
        {
            backgroundJobClient.Enqueue<PasswordResetEmailJob>(job => job.SendAsync(user.Id));
        }

        // Y-55: e-posta kayıtlı olsun olmasın AYNI cevap.
        return Result.Success(Messages.PasswordResetRequested);
    }

    public async Task<IResult> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var user = await accountGateway.FindByIdAsync(request.UserId).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Conflict(Messages.InvalidOrExpiredToken);
        }

        var reset = await accountGateway.ResetPasswordAsync(user, request.Token, request.NewPassword).ConfigureAwait(false);
        return reset ? Result.Success(Messages.PasswordResetSucceeded) : Result.Conflict(Messages.InvalidOrExpiredToken);
    }

    public async Task<IResult> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Unauthorized(Messages.UserNotFound);
        }

        var user = await accountGateway.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return Result.NotFound(Messages.UserNotFound);
        }

        var changed = await accountGateway.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword).ConfigureAwait(false);
        return changed ? Result.Success(Messages.PasswordChanged) : Result.Conflict(Messages.CurrentPasswordIncorrect);
    }

    public async Task<IDataResult<MeResponseDto>> GetMeAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return DataResult<MeResponseDto>.Unauthorized(Messages.UserNotFound);
        }

        var user = await accountGateway.FindByIdAsync(userId).ConfigureAwait(false);
        if (user is null)
        {
            return DataResult<MeResponseDto>.NotFound(Messages.UserNotFound);
        }

        var roles = await accountGateway.GetRoleNamesAsync(user).ConfigureAwait(false);
        var permissions = await identityGateway.GetPermissionsAsync(user).ConfigureAwait(false);

        var response = new MeResponseDto
        {
            Email = user.Email ?? string.Empty,
            Roles = roles,
            Permissions = permissions,
        };

        // Faz 19.3 (A-48): kayıt sırasında girilen öğrenci bilgileri bugüne kadar hiçbir yerde
        // görünmüyordu. Danışman/admin hesaplarında Student kaydı yok — alanlar null kalır.
        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is not null)
        {
            response.StudentNumber = student.StudentNumber;
            response.DepartmentId = student.DepartmentId;
            response.EnrollmentYear = student.EnrollmentYear;

            var department = await departmentRepository.GetAsync(d => d.Id == student.DepartmentId, cancellationToken).ConfigureAwait(false);
            if (department is not null)
            {
                response.DepartmentName = department.Name;

                var faculty = await facultyRepository.GetAsync(f => f.Id == department.FacultyId, cancellationToken).ConfigureAwait(false);
                response.FacultyName = faculty?.Name;
            }
        }

        return DataResult<MeResponseDto>.Success(response);
    }

    public async Task<IResult> UpdateMeAsync(UpdateMeRequestDto request, CancellationToken cancellationToken = default)
    {
        // Y-22: hedef kullanıcı istemciden değil token'dan — başkasının profili yazılamaz.
        if (currentUser.UserId is not { } userId)
        {
            return Result.Unauthorized(Messages.UserNotFound);
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Result.Forbidden(Messages.NotAStudent);
        }

        var department = await departmentRepository.GetAsync(d => d.Id == request.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (department is null)
        {
            return Result.NotFound(Messages.DepartmentNotFound);
        }

        // StudentNumber kasıtlı olarak yazılmaz (bkz. UpdateMeRequestDto).
        student.DepartmentId = department.Id;
        student.EnrollmentYear = request.EnrollmentYear;
        studentRepository.Update(student);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.ProfileUpdated);
    }

    public async Task<IDataResult<IReadOnlyCollection<RegistrationDepartmentDto>>> GetRegistrationDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        var departments = await departmentRepository.GetListAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        var facultyNames = (await facultyRepository.GetListAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            .ToDictionary(f => f.Id, f => f.Name);

        IReadOnlyCollection<RegistrationDepartmentDto> items = departments
            .Select(d => new RegistrationDepartmentDto { Id = d.Id, Name = d.Name, FacultyName = facultyNames.GetValueOrDefault(d.FacultyId, string.Empty) })
            .ToList();

        return DataResult<IReadOnlyCollection<RegistrationDepartmentDto>>.Success(items);
    }
}
