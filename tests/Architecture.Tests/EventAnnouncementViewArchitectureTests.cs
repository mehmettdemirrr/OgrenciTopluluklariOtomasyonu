using Xunit;

namespace Architecture.Tests;

/// <summary>docs/MIMARI.md · Y-89 (v6.18 tadili): etkinlik ve duyuru liste kartları tek bileşendedir.</summary>
public class EventAnnouncementViewArchitectureTests
{
    [Theory(DisplayName = "Y-89: etkinlik listesi sayfaları ortak EventCard'ı sarar")]
    [InlineData("EventsPage.tsx")]
    [InlineData("public/PublicEventsPage.tsx")]
    public void EventListPages_UseSharedCard(string relativePath)
    {
        var source = ReadPage(relativePath);

        Assert.True(
            source.Contains("EventCard", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} ortak etkinlik kartını kullanmıyor (A-85).");
        Assert.False(
            source.Contains("<CardMedia", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} kendi afişini çiziyor; afiş ortak bileşene aittir (A-85).");
    }

    [Theory(DisplayName = "Y-89: duyuru listesi sayfaları ortak AnnouncementGridCard'ı sarar")]
    [InlineData("AnnouncementsPage.tsx")]
    [InlineData("public/PublicAnnouncementsPage.tsx")]
    public void AnnouncementListPages_UseSharedCard(string relativePath)
    {
        var source = ReadPage(relativePath);

        Assert.True(
            source.Contains("AnnouncementGridCard", StringComparison.Ordinal),
            $"Y-89 ihlali — {relativePath} ortak duyuru kartını kullanmıyor (A-85).");
    }

    private static string ReadPage(string relativePath)
    {
        var path = Path.Combine(SolutionPaths.FindRepositoryRoot(), "arayuz", "src", "pages", relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), $"Sayfa bulunamadı: {path}");
        return File.ReadAllText(path);
    }
}
