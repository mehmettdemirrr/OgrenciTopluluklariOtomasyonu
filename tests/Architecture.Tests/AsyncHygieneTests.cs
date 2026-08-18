using Mono.Cecil;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-27: .Result, .Wait(), .GetAwaiter().GetResult(), async void yasak.
/// NetArchTest metot gövdesi tarayamadığı için doğrudan Mono.Cecil ile IL taranıyor.
/// Derleyicinin her `await` için ürettiği state machine (MoveNext) de aynı IL kalıbını
/// (GetAwaiter + GetResult) içerdiğinden, [CompilerGenerated] tipler taramanın dışında tutulur —
/// aksi halde her meşru `await` kullanımı yanlışlıkla ihlal olarak işaretlenir. Aynı sebeple,
/// üst düzey deyimlerde `await` kullanan bir giriş noktası (WebAPI/Program.cs) için Roslyn'in
/// ürettiği eşzamanlı `<Main>` sarmalayıcısı da (modülün EntryPoint'i — işletim sisteminin
/// eşzamanlı süreç girişini async Main'e bağlayan tek seferlik, kaçınılmaz köprü) hariç tutulur.
/// </summary>
public class AsyncHygieneTests
{
    private static readonly string[] AssembliesToScan = ["Core", "Entities", "DataAccess", "Business", "WebAPI"];

    [Fact(DisplayName = "Y-27: Task.Result, Task.Wait() veya GetAwaiter().GetResult() ile senkron bloklama yapılamaz")]
    public void Async_Kod_Senkron_Olarak_Bloklanamaz()
    {
        var violations = new List<string>();

        foreach (var assemblyName in AssembliesToScan)
        {
            var path = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
            using var assembly = AssemblyDefinition.ReadAssembly(path);

            var entryPoint = assembly.MainModule.EntryPoint;

            foreach (var type in AllTypes(assembly.MainModule.Types).Where(t => !IsCompilerGenerated(t)))
            {
                foreach (var method in type.Methods.Where(m => m.HasBody && m != entryPoint))
                {
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.Operand is not MethodReference calledMethod)
                        {
                            continue;
                        }

                        if (IsBlockingTaskCall(calledMethod))
                        {
                            violations.Add($"{assemblyName}: {type.FullName}.{method.Name} -> {calledMethod.DeclaringType.Name}.{calledMethod.Name}");
                        }
                    }
                }
            }
        }

        Assert.True(violations.Count == 0, "Senkron bloklama bulundu:\n" + string.Join("\n", violations));
    }

    [Fact(DisplayName = "Y-27: async void kullanılamaz (event handler istisnası hariç)")]
    public void Async_Void_Kullanilamaz()
    {
        var violations = new List<string>();

        foreach (var assemblyName in AssembliesToScan)
        {
            var path = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
            using var assembly = AssemblyDefinition.ReadAssembly(path);

            foreach (var type in AllTypes(assembly.MainModule.Types).Where(t => !IsCompilerGenerated(t)))
            {
                foreach (var method in type.Methods)
                {
                    var isAsyncStateMachine = method.CustomAttributes
                        .Any(a => a.AttributeType.Name == "AsyncStateMachineAttribute");
                    var returnsVoid = method.ReturnType.FullName == "System.Void";
                    var looksLikeEventHandler = method.Parameters.Count == 2
                        && method.Parameters[1].ParameterType.Name.EndsWith("EventArgs", StringComparison.Ordinal);

                    if (isAsyncStateMachine && returnsVoid && !looksLikeEventHandler)
                    {
                        violations.Add($"{assemblyName}: {type.FullName}.{method.Name}");
                    }
                }
            }
        }

        Assert.True(violations.Count == 0, "async void bulundu:\n" + string.Join("\n", violations));
    }

    private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
    {
        foreach (var type in types)
        {
            yield return type;

            foreach (var nested in AllTypes(type.NestedTypes))
            {
                yield return nested;
            }
        }
    }

    private static bool IsCompilerGenerated(TypeDefinition type) =>
        type.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute");

    private static bool IsBlockingTaskCall(MethodReference method)
    {
        var declaringType = method.DeclaringType?.FullName ?? string.Empty;

        var isTaskResult = method.Name == "get_Result"
            && declaringType.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal);

        var isTaskWait = method.Name == "Wait"
            && declaringType.StartsWith("System.Threading.Tasks.Task", StringComparison.Ordinal);

        var isAwaiterGetResult = method.Name == "GetResult"
            && declaringType.Contains("TaskAwaiter", StringComparison.Ordinal);

        return isTaskResult || isTaskWait || isAwaiterGetResult;
    }
}
