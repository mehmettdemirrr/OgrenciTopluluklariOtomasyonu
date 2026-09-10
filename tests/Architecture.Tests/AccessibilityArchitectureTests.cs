using Xunit;

namespace Architecture.Tests;

/// <summary>docs/MIMARI.md · Y-84: erişilebilirlik tercihi bileşenlerde okunmaz, temadan gelir.</summary>
public class AccessibilityArchitectureTests
{
    [Fact(DisplayName = "Y-84: useAccessibility yalnızca a11y klasöründe ve tema sağlayıcısında kullanılır")]
    public void Preferences_AreReadOnlyByProviderAndTheme()
    {
        var sourceRoot = Path.Combine(SolutionPaths.FindRepositoryRoot(), "arayuz", "src");
        Assert.True(Directory.Exists(sourceRoot), $"Kaynak dizini bulunamadı: {sourceRoot}");

        var allowed = new[]
        {
            Path.Combine("a11y", "AccessibilityContext.tsx"),
            Path.Combine("a11y", "AccessibilityFab.tsx"),
            Path.Combine("a11y", "AccessibilityMenu.tsx"),
            Path.Combine("a11y", "AccessibilityOverlays.tsx"),
            Path.Combine("theme", "ThemeModeContext.tsx"),
        };

        var offenders = Directory
            .EnumerateFiles(sourceRoot, "*.tsx", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(sourceRoot, "*.ts", SearchOption.AllDirectories))
            .Where(path => File.ReadAllText(path).Contains("useAccessibility(", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(sourceRoot, path))
            .Where(relative => !allowed.Contains(relative, StringComparer.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"Y-84 ihlali — erişilebilirlik tercihi bileşende okunuyor: {string.Join(", ", offenders)}. " +
            "Sonucu temadan al (A-79); tercihi bileşende okuma.");
    }
}
