namespace Architecture.Tests;

internal static class SolutionPaths
{
    public static string FindSrcDirectory() => Path.Combine(FindRepositoryRoot(), "src");

    /// <summary>docs/MIMARI.md · Y-78: RichTextSafetyTests bu kökten `arayuz/src`'i tarar.</summary>
    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && directory.GetFiles("*.slnx").Length == 0)
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Çözüm kökü (*.slnx) bulunamadı; test çalışma dizininden yukarı doğru arandı: " + AppContext.BaseDirectory);
        }

        return directory.FullName;
    }
}
