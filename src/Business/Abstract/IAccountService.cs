using Business.DTOs.Auth;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>docs/PLAN-V2.md · Faz 11 (K-03/A-40): hesap yaşam döngüsü — kayıt, e-posta doğrulama, şifre sıfırlama.</summary>
public interface IAccountService
{
    /// <summary>
    /// docs/PLAN-V2.md · Y-46: [TransactionAspect] kasıtlı olarak kullanılmaz — commit'ten SONRA
    /// Hangfire'a doğrulama e-postası işi eklenebilmesi için transaction elle yönetilir (bkz. AccountManager).
    /// </summary>
    [ValidationAspect(typeof(RegisterRequestValidator))]
    Task<IResult> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);

    Task<IResult> ConfirmEmailAsync(ConfirmEmailRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Y-55 ailesi: e-posta kayıtlı olsun olmasın, zaten doğrulanmış olsun olmasın aynı cevabı döner.</summary>
    [ValidationAspect(typeof(ResendConfirmationRequestValidator))]
    Task<IResult> ResendConfirmationAsync(ResendConfirmationRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Y-55: e-posta kayıtlı olsun olmasın aynı cevabı döner (kullanıcı sayımı sızıntısı önlenir).</summary>
    [ValidationAspect(typeof(ForgotPasswordRequestValidator))]
    Task<IResult> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);

    [ValidationAspect(typeof(ResetPasswordRequestValidator))]
    [TransactionAspect]
    Task<IResult> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Yalnızca kimliği doğrulanmış kullanıcı — Y-21 fallback policy zaten şart koşuyor.</summary>
    [ValidationAspect(typeof(ChangePasswordRequestValidator))]
    [TransactionAspect]
    Task<IResult> ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken cancellationToken = default);

    Task<IDataResult<MeResponseDto>> GetMeAsync(CancellationToken cancellationToken = default);

    /// <summary>docs/PLAN-V2.md: kayıt formunun bölüm seçimi — anonim, dar izdüşüm (reference.manage gerektirmez).</summary>
    Task<IDataResult<IReadOnlyCollection<RegistrationDepartmentDto>>> GetRegistrationDepartmentsAsync(CancellationToken cancellationToken = default);
}
