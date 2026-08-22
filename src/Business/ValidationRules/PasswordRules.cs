using System.Text.RegularExpressions;

namespace Business.ValidationRules;

/// <summary>
/// docs/PLAN-V2.md · Faz 11: Identity'nin varsayılan PasswordOptions'ıyla (RequireDigit/RequireLowercase/
/// RequireUppercase/RequireNonAlphanumeric/RequiredLength=6) birebir aynı kural — sunucu tarafı
/// FluentValidation reddi ile Identity'nin kendi reddi arasında sürpriz farkı olmasın diye tek yerde.
/// </summary>
public static partial class PasswordRules
{
    public static bool IsStrongEnough(string password) => StrongPasswordRegex().IsMatch(password);

    [GeneratedRegex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{6,}$")]
    private static partial Regex StrongPasswordRegex();
}
