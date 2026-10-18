using System.IO.Compression;

namespace NewEdenKit.Sde.Tests;

/// <summary>
/// A small SDE sample taken from build 3607705: a handful of real records per typed dataset,
/// chosen so that together they contain every field that appears in the full export.
/// </summary>
internal static class TestSde
{
    public const long BuildNumber = 3607705;

    public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "TestData", "sde");

    /// <summary>Zips the sample into <paramref name="path"/>, named like a real archive when <paramref name="path"/> is a directory.</summary>
    public static string CreateArchive(string directory, long buildNumber = BuildNumber)
    {
        System.IO.Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"eve-online-static-data-{buildNumber}-jsonl.zip");
        if (File.Exists(path))
            File.Delete(path);
        ZipFile.CreateFromDirectory(Directory, path);
        return path;
    }
}

/// <summary>A temporary directory deleted on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NewEdenKit.Sde.Tests", Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
