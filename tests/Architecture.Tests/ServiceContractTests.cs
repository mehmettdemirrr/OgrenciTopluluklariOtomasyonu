using System.Reflection;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-30: Servis imzası object, dynamic veya anonim tip dönemez.
/// (dynamic, CLR düzeyinde object olarak derlenir; object dönüş tipini yakalamak ikisini de kapsar.)
/// </summary>
public class ServiceContractTests
{
    [Fact(DisplayName = "Y-30: Business katmanındaki public arayüz metotları object/dynamic dönemez")]
    public void Business_Arayuzleri_Object_Donemez()
    {
        var businessAssembly = Assembly.Load("Business");

        var serviceInterfaces = businessAssembly.GetTypes()
            .Where(t => t.IsInterface && t.IsPublic);

        var violations = new List<string>();

        foreach (var serviceInterface in serviceInterfaces)
        {
            foreach (var method in serviceInterface.GetMethods())
            {
                if (UnwrapTaskResult(method.ReturnType) == typeof(object))
                {
                    violations.Add($"{serviceInterface.Name}.{method.Name}(...) : object/dynamic dönüyor");
                }
            }
        }

        Assert.True(violations.Count == 0, string.Join("\n", violations));
    }

    private static Type UnwrapTaskResult(Type type) =>
        type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(Task<>)
            ? type.GetGenericArguments()[0]
            : type;
}
