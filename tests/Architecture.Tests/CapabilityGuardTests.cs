using Mono.Cecil;
using Mono.Cecil.Cil;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-75: kulüp içi kapasite matrisi yalnızca DARALTIR, asla genişletmez.
/// Uçtaki [SecuredOperation] birinci kapıdır; kapasite ikincisi. Sıra bozulursa kulüp başkanı
/// kendi kulübüne, sistemde hiç var olmayan bir yetkiyi dağıtabilir hâle gelir (Y-37).
///
/// Test şunu doğrular: bir controller'a enjekte edilen servis arayüzünün, <b>kendi tipi içinde</b>
/// <c>ClubMembership.Capabilities</c> okuyan her metodu [SecuredOperation] taşır. Altı kapının
/// (<c>Ensure*Access*</c>) hepsi manager'ının private üyesidir, dolayısıyla bu kapsam onları tam kaplar.
///
/// <b>Kasıtlı olarak kapsam dışı:</b>
/// <list type="bullet">
/// <item><c>IReportScopeResolver</c> — controller'a enjekte edilmez; "iç bileşen, aspect taşımaz".</item>
/// <item><c>IDashboardService.GetSummaryAsync</c> — [SecuredOperation]'sız olması K-25 kararıdır;
/// oradaki kapasite okuması bir izin KAPISI değil, kullanıcının kendi özetinde hangi kulüplerin
/// görüneceğini belirleyen bir KAPSAM hesabıdır (A-55/A-67 yönetir).</item>
/// </list>
/// Y-75 izin kapılarını korur; kapsam çözümlemesi ayrı bir konudur.
///
/// ScopeGuardTests (Y-66/Y-74) ile aynı sınıftan bir koruma — kural derleme sonrası IL'de aranır.
/// </summary>
public class CapabilityGuardTests
{
    private const string CapabilityGetter = "get_Capabilities";
    private const string MembershipType = "Entities.ClubMembership";
    private const string SecuredOperationAttribute = "SecuredOperationAttribute";

    [Fact(DisplayName = "Y-75: kapasite kapısı olan her controller servisi metodu [SecuredOperation] taşır")]
    public void KapasiteOkuyanMetotlar_SecuredOperation_Tasir()
    {
        // ScopeGuardTests ile aynı desen: derlenmiş çıktıyı yoldan oku, tipe bağımlılık kurma.
        using var business = AssemblyDefinition.ReadAssembly(Path.Combine(AppContext.BaseDirectory, "Business.dll"));
        using var webApi = AssemblyDefinition.ReadAssembly(Path.Combine(AppContext.BaseDirectory, "WebAPI.dll"));

        var concreteTypes = business.MainModule.Types
            .Where(t => t.Namespace == "Business.Concrete" && t.IsClass)
            .ToList();

        // Y-75'in koruduğu şey bir UÇ'tur: tehlike, bir controller ucunun tek kapısının kulüp
        // matrisi olması. Controller'a enjekte EDİLMEYEN arayüzler (ör. IReportScopeResolver —
        // "iç bileşen, aspect taşımaz") kendi başlarına bir uç değildir; onlara ulaşan controller
        // servisi metodu üzerinden zaten kapsanırlar.
        var controllerInjectedInterfaces = webApi.MainModule.Types
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .SelectMany(t => t.Methods.Where(m => m.IsConstructor))
            .SelectMany(m => m.Parameters)
            .Select(p => p.ParameterType.Name)
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();

        foreach (var interfaceType in business.MainModule.Types.Where(t => t.Namespace == "Business.Abstract" && t.IsInterface))
        {
            if (!controllerInjectedInterfaces.Contains(interfaceType.Name))
            {
                continue;
            }

            var implementation = concreteTypes.FirstOrDefault(t => t.Interfaces.Any(i => i.InterfaceType.Name == interfaceType.Name));
            if (implementation is null)
            {
                continue;
            }

            foreach (var interfaceMethod in interfaceType.Methods)
            {
                if (!ReachesCapability(implementation, interfaceMethod.Name))
                {
                    continue;
                }

                var secured = interfaceMethod.CustomAttributes
                    .Any(a => a.AttributeType.Name == SecuredOperationAttribute);

                if (!secured)
                {
                    violations.Add($"{interfaceType.Name}.{interfaceMethod.Name}");
                }
            }
        }

        Assert.True(
            violations.Count == 0,
            "Y-75 ihlali — kulüp kapasitesi kapısı olan şu metotlar [SecuredOperation] taşımıyor, " +
            "yani yetki kararı yalnızca kulüp matrisine bırakılmış: " + string.Join(", ", violations) +
            ". Bkz. docs/MIMARI.md Y-75 / A-68 — matris bir FİLTRE, bir KAYNAK değil.");
    }

    /// <summary>
    /// Giriş metodunun kendisi ya da <b>aynı tipteki</b> bir yardımcısı kapasite okuyor mu?
    /// Kapı metotları (<c>Ensure*Access*</c>) manager'ın private üyeleridir ve arayüz metodundan
    /// doğrudan çağrılır — bir seviye derinlik altı kapının hepsini kapsar.
    /// </summary>
    private static bool ReachesCapability(TypeDefinition type, string entryMethodName)
    {
        var entries = type.Methods.Where(m => StripCompilerNoise(m.Name) == entryMethodName).ToList();
        if (entries.Count == 0)
        {
            return false;
        }

        if (entries.Any(ReadsCapabilities))
        {
            return true;
        }

        var calledNames = entries
            .SelectMany(EffectiveBodies)
            .SelectMany(b => b.Instructions)
            .Select(i => i.Operand as MethodReference)
            .Where(r => r is not null && r.DeclaringType.FullName == type.FullName)
            .Select(r => StripCompilerNoise(r!.Name))
            .ToHashSet(StringComparer.Ordinal);

        return type.Methods
            .Where(m => calledNames.Contains(StripCompilerNoise(m.Name)))
            .Any(ReadsCapabilities);
    }

    /// <summary>Metot gövdesi (state machine ve closure'lar dâhil) ClubMembership.Capabilities okuyor mu?</summary>
    private static bool ReadsCapabilities(MethodDefinition method) =>
        EffectiveBodies(method).Any(body => body.Instructions.Any(i =>
            i.Operand is MethodReference reference
            && reference.Name == CapabilityGetter
            && reference.DeclaringType.FullName == MembershipType));

    /// <summary>Metodun kendi gövdesi + async state machine MoveNext + iç içe closure gövdeleri.</summary>
    private static IEnumerable<MethodBody> EffectiveBodies(MethodDefinition method)
    {
        if (method.HasBody)
        {
            yield return method.Body;
        }

        var stateMachines = method.DeclaringType.NestedTypes
            .Where(n => n.Name.Contains($"<{StripCompilerNoise(method.Name)}>", StringComparison.Ordinal));

        foreach (var nested in stateMachines)
        {
            foreach (var nestedMethod in nested.Methods.Where(m => m.HasBody))
            {
                yield return nestedMethod.Body;
            }
        }
    }

    /// <summary><c>&lt;GetMembersPagedAsync&gt;d__12</c> gibi derleyici adlarından asıl adı çıkarır.</summary>
    private static string StripCompilerNoise(string name)
    {
        var start = name.IndexOf('<', StringComparison.Ordinal);
        if (start < 0)
        {
            return name;
        }

        var end = name.IndexOf('>', start);
        return end < 0 ? name : name[(start + 1)..end];
    }
}
