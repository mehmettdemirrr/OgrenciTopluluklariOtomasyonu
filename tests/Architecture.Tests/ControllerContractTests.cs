using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Y-09: Entity sınıfı HTTP request/response gövdesinde yer alamaz.
/// </summary>
public class ControllerContractTests
{
    [Fact(DisplayName = "Y-09: Controller action'ları Entities katmanındaki tipleri parametre/dönüş değeri olarak kullanamaz")]
    public void Controller_Actionlari_Entity_Tipi_Kullanamaz()
    {
        var webApiAssembly = Assembly.Load("WebAPI");
        var entitiesAssembly = Assembly.Load("Entities");

        var controllerTypes = webApiAssembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract);

        var violations = new List<string>();

        foreach (var controllerType in controllerTypes)
        {
            var actions = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            foreach (var action in actions)
            {
                foreach (var parameter in action.GetParameters())
                {
                    if (UnwrapGeneric(parameter.ParameterType).Assembly == entitiesAssembly)
                    {
                        violations.Add($"{controllerType.Name}.{action.Name}(...) parametresi: {parameter.ParameterType.Name}");
                    }
                }

                if (UnwrapGeneric(action.ReturnType).Assembly == entitiesAssembly)
                {
                    violations.Add($"{controllerType.Name}.{action.Name}(...) dönüş tipi: {action.ReturnType.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0, "Entity sızıntısı bulundu:\n" + string.Join("\n", violations));
    }

    /// <summary>Task&lt;T&gt;, ActionResult&lt;T&gt; gibi sarmalayıcıların içindeki gerçek tipi çıkarır.</summary>
    private static Type UnwrapGeneric(Type type)
    {
        while (type.IsGenericType)
        {
            var genericArgument = type.GetGenericArguments().FirstOrDefault();
            if (genericArgument is null)
            {
                break;
            }

            type = genericArgument;
        }

        return type;
    }
}
