using Xunit;

namespace Architecture.Tests;

/// <summary>docs/MIMARI.md · Y-89: kulüp kartı ve künyesi tek bileşendedir; sayfalar kendi kartını çizmez.</summary>
public class ClubViewArchitectureTests
{
    [Theory(DisplayName = "Y-89: kulüp listesi sayfaları ortak ClubCard'ı sarar")]
    [InlineData("ClubsPage.tsx")]
    [InlineData("public/PublicClubsPage.tsx")]
    public void ClubListPages_UseSharedCard(string relativePath)
    {
        var source = ReadPage(relativePath);

        Assert.True(
            source.Contains("ClubCard", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} ortak kartı kullanmıyor; components/clubs/ClubCard.tsx sarılmalı (A-85).");
        Assert.False(
            source.Contains("<CardMedia", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} kendi kart görselini çiziyor; görsel ortak bileşene aittir (A-85).");
    }

    [Theory(DisplayName = "Y-89: kulüp detay sayfaları ortak ClubProfileHeader'ı sarar")]
    [InlineData("ClubDetailPage.tsx")]
    [InlineData("public/PublicClubDetailPage.tsx")]
    public void ClubDetailPages_UseSharedHeader(string relativePath)
    {
        var source = ReadPage(relativePath);

        Assert.True(
            source.Contains("ClubProfileHeader", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} ortak künyeyi kullanmıyor (A-85).");
        Assert.False(
            source.Contains("DetailHero", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} künyeyi DetailHero ile ayrı çiziyor; ortak bileşene taşı (A-85).");
    }

    private static string ReadPage(string relativePath)
    {
        var path = Path.Combine(SolutionPaths.FindRepositoryRoot(), "arayuz", "src", "pages", relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Sayfa bulunamadı: {path}");
        return File.ReadAllText(path);
    }
}
