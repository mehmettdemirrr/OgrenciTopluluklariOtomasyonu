using Mono.Cecil;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-66 / A-55: kulüp kapsamı kontrol eden her metot, kontrolün en başında
/// <c>clubs.manage.all</c> iznine bakmak zorundadır.
///
/// Bu kuralı yorumla korumak yetmez: satır unutulduğunda hiçbir test kırılmaz, yalnızca yönetici
/// kendi yönettiği sistemde sessizce kilitlenir ve hata mesajı ("bu topluluğun danışmanı ya da
/// başkanı olmanız gerekir") sebebi söylemez. v4.0'da tam olarak bu olmuştu: 7 kapsam metodunun
/// 4'ünde yönetici yolu hiç yoktu.
///
/// AsyncHygieneTests (Y-27) ile aynı sınıftan bir koruma — kural derleme sonrası IL'de aranır.
/// </summary>
public class ScopeGuardTests
{
    private const string RequiredPermission = "clubs.manage.all";

    /// <summary>Kapsam metotlarının adlandırma sözleşmesi; yeni bir tanesi eklenince test onu da kapsar.</summary>
    private const string ScopeMethodPrefix = "Ensure";

    private static readonly string[] ScopeMethodSuffixes = ["AccessAsync", "AdvisorAsync"];

    [Fact(DisplayName = "Y-66: her Ensure*Access metodu clubs.manage.all iznine bakar (yönetici sessizce kilitlenemez)")]
    public void Kapsam_Metotlari_Yonetici_Iznini_Kontrol_Eder()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Business.dll");
        using var assembly = AssemblyDefinition.ReadAssembly(path);

        var scopeMethods = assembly.MainModule.Types
            .Where(t => t.Namespace == "Business.Concrete")
            .SelectMany(t => t.Methods.Where(m => m.HasBody && IsScopeMethod(m.Name)).Select(m => (Type: t, Method: m)))
            .ToList();

        // Testin kendisi anlamsızlaşmasın: metot bulunamıyorsa adlandırma sözleşmesi değişmiştir.
        Assert.True(
            scopeMethods.Count >= 7,
            $"Beklenen en az 7 kapsam metodu, bulunan {scopeMethods.Count}. Adlandırma sözleşmesi mi değişti?");

        var violations = scopeMethods
            .Where(entry => !LoadsPermission(entry.Method, RequiredPermission))
            .Select(entry => $"{entry.Type.Name}.{entry.Method.Name}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            $"Kapsam metodu '{RequiredPermission}' iznini kontrol etmiyor — yönetici bu uçta kilitli kalır (Y-66):\n"
                + string.Join("\n", violations));
    }

    private static bool IsScopeMethod(string name) =>
        name.StartsWith(ScopeMethodPrefix, StringComparison.Ordinal)
        && ScopeMethodSuffixes.Any(suffix => name.EndsWith(suffix, StringComparison.Ordinal));

    /// <summary>
    /// İzin kodu sabiti (<c>ldstr</c>) metodun GERÇEK gövdesinde geçiyor mu.
    ///
    /// Kapsam metotlarının hepsi <c>async</c>: derleyici gövdeyi bir state machine tipine taşır ve
    /// geriye yalnızca onu kuran bir saplama bırakır. Doğrudan <c>method.Body</c>'ye bakan bir
    /// tarama bu yüzden 7 metodun 7'sini de "ihlal" olarak işaretler (AsyncHygieneTests'in ters
    /// yönde tarif ettiği aynı tuzak).
    /// </summary>
    private static bool LoadsPermission(MethodDefinition method, string permission)
    {
        if (ContainsString(method, permission))
        {
            return true;
        }

        var stateMachine = method.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.Name == "AsyncStateMachineAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as TypeReference;

        var moveNext = stateMachine?.Resolve()?.Methods.FirstOrDefault(m => m.Name == "MoveNext");

        return moveNext is not null && ContainsString(moveNext, permission);
    }

    private static bool ContainsString(MethodDefinition method, string value) =>
        method.HasBody && method.Body.Instructions.Any(instruction => instruction.Operand as string == value);
}
