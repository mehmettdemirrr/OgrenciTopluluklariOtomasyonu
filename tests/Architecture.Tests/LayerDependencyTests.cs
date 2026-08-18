using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Architecture.Tests;

/// <summary>
/// docs/MIMARI.md · Bölüm 1 (katman bağımlılık yönü) ve Bölüm 2: Y-01, Y-02, Y-05, Y-07, Y-08.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly CoreAssembly = Assembly.Load("Core");
    private static readonly Assembly EntitiesAssembly = Assembly.Load("Entities");
    private static readonly Assembly BusinessAssembly = Assembly.Load("Business");
    private static readonly Assembly WebApiAssembly = Assembly.Load("WebAPI");

    [Fact(DisplayName = "Y-02: Core, Entities dışında hiçbir katmana bağımlı olamaz")]
    public void Core_Katmani_Ust_Katmanlara_Bagimli_Olamaz()
    {
        var result = Types.InAssembly(CoreAssembly)
            .Should()
            .NotHaveDependencyOnAny("Entities", "DataAccess", "Business", "WebAPI")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact(DisplayName = "Y-02: Entities, DataAccess/Business/WebAPI'ye bağımlı olamaz")]
    public void Entities_Katmani_Ust_Katmanlara_Bagimli_Olamaz()
    {
        var result = Types.InAssembly(EntitiesAssembly)
            .Should()
            .NotHaveDependencyOnAny("DataAccess", "Business", "WebAPI")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact(DisplayName = "Y-01 / Y-05: WebAPI, DataAccess katmanını atlayarak kullanamaz")]
    public void WebApi_DataAccess_Katmanini_Atlayamaz()
    {
        var result = Types.InAssembly(WebApiAssembly)
            .Should()
            .NotHaveDependencyOn("DataAccess")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact(DisplayName = "Y-01 / Y-08: EF Core tipleri yalnızca DataAccess katmanında yaşayabilir")]
    public void EfCore_Sadece_DataAccess_Katmaninda_Kullanilabilir()
    {
        var result = Types.InAssemblies([CoreAssembly, EntitiesAssembly, BusinessAssembly, WebApiAssembly])
            .Should()
            .NotHaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, Describe(result));
    }

    [Fact(DisplayName = "Y-07: src/ altında yalnızca beş katman projesi bulunur")]
    public void Cozum_Tam_Bes_Katman_Projesinden_Olusur()
    {
        var srcDirectory = SolutionPaths.FindSrcDirectory();

        var actualProjects = Directory.GetDirectories(srcDirectory)
            .Select(Path.GetFileName)
            .Where(name => name is not null && File.Exists(Path.Combine(srcDirectory, name, $"{name}.csproj")))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        string[] expectedProjects = ["Business", "Core", "DataAccess", "Entities", "WebAPI"];

        Assert.Equal(expectedProjects, actualProjects);
    }

    private static string Describe(TestResult result) =>
        "İhlal eden tipler: " + string.Join(", ", result.FailingTypeNames ?? []);
}
