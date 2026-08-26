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

    /// <summary>
    /// docs/MIMARI.md · A-55/A-67: yöneticinin kapsamdan çıkabildiği izinler. İkisi bilinçli olarak
    /// ayrıdır — <c>clubs.manage.all</c> kulüp kapsamını, <c>reports.read.all</c> ise YALNIZCA rapor
    /// kapsamını (<c>ReportScopeResolver</c>) yönetir. A-55 bu ayrımı kurarken "reports.read.all'ın
    /// fiilen admin bayrağı olarak kullanılması sona erer" demişti; bu liste o ayrımı korur.
    /// </summary>
    private static readonly string[] AdminEscapePermissions = [RequiredPermission, "reports.read.all"];

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
    /// docs/MIMARI.md · Y-74 / A-67: adlandırmadan bağımsız tarama.
    ///
    /// Yukarıdaki test kapsam metotlarını <b>ada göre</b> (<c>Ensure*Access</c>) buluyor. Kapsamı bir
    /// sorgunun içine gömen metotlar bu yüzden görünmez kalıyordu: Faz 30'un elle doğrulamasında
    /// yöneticinin hem etkinlik hem üyelik onay kuyruğunu <b>sessizce boş</b> gördüğü ortaya çıktı —
    /// dört metot testin kör noktasındaydı.
    ///
    /// Kapsam daraltmanın gerçek imzası adı değil, <b>"ben kimim"</b> sorusudur:
    /// <c>AcademicStaff.ApplicationUserId == currentUser.UserId</c>. Buna karşılık
    /// <c>s.Id == request.AdvisorId</c> ("verilen danışman var mı") zararsız bir doğrulamadır ve
    /// <c>AcademicStaff::get_ApplicationUserId</c>'ye hiç dokunmadığı için bu taramaya takılmaz.
    /// </summary>
    [Fact(DisplayName = "Y-74: çağıranı danışmana çözen her metot clubs.manage.all yolunu taşır (ada bakılmaz)")]
    public void Caginani_Danismana_Cozen_Metotlar_Yonetici_Iznini_Kontrol_Eder()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Business.dll");
        using var assembly = AssemblyDefinition.ReadAssembly(path);

        var advisorScopedMethods = assembly.MainModule.Types
            .Where(t => t.Namespace == "Business.Concrete")
            .SelectMany(t => t.Methods
                .Where(m => m.HasBody && !IsCompilerGenerated(m.DeclaringType) && ResolvesCallerToAdvisor(m))
                .Select(m => (Type: t, Method: m)))
            .ToList();

        // Testin kendisi anlamsızlaşmasın: hiç metot bulunamıyorsa tarama bozulmuştur.
        Assert.True(
            advisorScopedMethods.Count >= 5,
            $"Çağıranı danışmana çözen en az 5 metot bekleniyordu, bulunan {advisorScopedMethods.Count}. "
                + "IL taraması mı bozuldu?");

        var violations = advisorScopedMethods
            .Where(entry => !AdminEscapePermissions.Any(permission => LoadsPermission(entry.Method, permission)))
            .Select(entry => $"{entry.Type.Name}.{entry.Method.Name}")
            .ToList();

        Assert.True(
            violations.Count == 0,
            "Çağıranı danışmana çözen metot hiçbir yönetici çıkışı taşımıyor — yönetici bu uçta "
                + $"sessizce kilitli kalır (Y-74/A-67):\n{string.Join("\n", violations)}");
    }

    /// <summary>
    /// Metot çağıranın KENDİSİNİ danışmana çözüyor mu: hem <c>AcademicStaff.ApplicationUserId</c>
    /// okunmalı hem de <c>ICurrentUser.UserId</c>'ye dokunulmalı.
    ///
    /// İki koşul birlikte aranır, çünkü tek başına <c>ApplicationUserId</c> yetmiyor:
    /// <c>ReferenceDataManager.CreateAcademicStaffAsync</c> onu <c>request.ApplicationUserId</c> ile
    /// (A-14 tekillik kontrolü), <c>RoleAdminManager.FindDeletionBlockerAsync</c> ise SİLİNECEK
    /// kullanıcının kimliğiyle karşılaştırır. İkisi de kapsam daraltmaz; yönetici çıkışı beklemek yanlış olur.
    /// </summary>
    private static bool ResolvesCallerToAdvisor(MethodDefinition method) =>
        ReferencesMember(method, "Entities.AcademicStaff", "get_ApplicationUserId")
        && ReferencesMember(method, "Core.Utilities.Security.ICurrentUser", "get_UserId");

    private static bool ReferencesMember(MethodDefinition method, string declaringTypeFullName, string memberName) =>
        EffectiveBodies(method).Any(body => body.Body.Instructions.Any(instruction =>
            instruction.Operand is MethodReference reference
            && reference.Name == memberName
            && reference.DeclaringType?.FullName == declaringTypeFullName));

    private static bool IsCompilerGenerated(TypeDefinition type) =>
        type.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute");

    /// <summary>
    /// İzin kodu sabiti (<c>ldstr</c>) metodun GERÇEK gövdesinde geçiyor mu.
    ///
    /// Kapsam metotlarının hepsi <c>async</c>: derleyici gövdeyi bir state machine tipine taşır ve
    /// geriye yalnızca onu kuran bir saplama bırakır. Doğrudan <c>method.Body</c>'ye bakan bir
    /// tarama bu yüzden 7 metodun 7'sini de "ihlal" olarak işaretler (AsyncHygieneTests'in ters
    /// yönde tarif ettiği aynı tuzak).
    /// </summary>
    private static bool LoadsPermission(MethodDefinition method, string permission) =>
        EffectiveBodies(method).Any(body =>
            body.Body.Instructions.Any(instruction => instruction.Operand as string == permission));

    /// <summary>
    /// Metodun GERÇEK gövdesi: kendisi + async state machine'inin MoveNext'i + derleyicinin ürettiği
    /// closure metotları. Yalnızca <c>method.Body</c>'ye bakmak yanıltır — async metotlarda gövde
    /// state machine'e taşınır, lambda'lar ise ayrı closure tiplerine.
    /// </summary>
    private static IEnumerable<MethodDefinition> EffectiveBodies(MethodDefinition method)
    {
        var seen = new HashSet<MethodDefinition>();
        var pending = new Stack<MethodDefinition>();
        pending.Push(method);

        var stateMachine = method.CustomAttributes
            .FirstOrDefault(a => a.AttributeType.Name == "AsyncStateMachineAttribute")
            ?.ConstructorArguments.FirstOrDefault().Value as TypeReference;

        if (stateMachine?.Resolve()?.Methods.FirstOrDefault(m => m.Name == "MoveNext") is { } moveNext)
        {
            pending.Push(moveNext);
        }

        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!current.HasBody || !seen.Add(current))
            {
                continue;
            }

            yield return current;

            foreach (var instruction in current.Body.Instructions)
            {
                if (instruction.Operand is not MethodReference reference)
                {
                    continue;
                }

                var declaring = TryResolve(reference.DeclaringType);
                if (declaring is null || !declaring.IsNested || !IsCompilerGenerated(declaring))
                {
                    continue;
                }

                if (reference.Resolve() is { } target)
                {
                    pending.Push(target);
                }
            }
        }
    }

    private static TypeDefinition? TryResolve(TypeReference? type)
    {
        try
        {
            return type?.Resolve();
        }
        catch (AssemblyResolutionException)
        {
            return null;
        }
    }
}
