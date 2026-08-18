namespace Architecture.Tests;

internal static class SolutionPaths
{
    public static string FindSrcDirectory()
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

        return Path.Combine(directory.FullName, "src");
    }
}
