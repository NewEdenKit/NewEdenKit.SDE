using System.Globalization;
using System.Text.RegularExpressions;

namespace NewEdenKit.Sde;

/// <summary>
/// A local directory of downloaded SDE archives, named the same way CCP names them
/// (<c>eve-online-static-data-&lt;build&gt;-&lt;variant&gt;.zip</c>).
/// </summary>
public sealed partial class SdeArchiveCache
{
    /// <summary>Uses <paramref name="directory"/> as the cache. It is created when needed.</summary>
    public SdeArchiveCache(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        Directory = Path.GetFullPath(directory);
    }

    /// <summary>The default cache directory: <c>%LOCALAPPDATA%/NewEdenKit/Sde</c> (or the platform equivalent).</summary>
    public static string DefaultDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), "NewEdenKit", "Sde");

    /// <summary>The full path of the cache directory.</summary>
    public string Directory { get; }

    /// <summary>Where the archive for a build is stored, whether or not it exists yet.</summary>
    public string GetArchivePath(long buildNumber, SdeVariant variant = SdeVariant.JsonLines) =>
        Path.Combine(Directory, variant.GetArchiveFileName(buildNumber));

    /// <summary>Whether the archive for a build is in the cache.</summary>
    public bool Contains(long buildNumber, SdeVariant variant = SdeVariant.JsonLines) =>
        File.Exists(GetArchivePath(buildNumber, variant));

    /// <summary>The build numbers in the cache, newest first.</summary>
    public IReadOnlyList<long> GetCachedBuilds(SdeVariant variant = SdeVariant.JsonLines)
    {
        if (!System.IO.Directory.Exists(Directory))
            return [];

        var suffix = variant.ToFileSuffix();
        return System.IO.Directory.EnumerateFiles(Directory, "eve-online-static-data-*.zip")
            .Select(path => ArchiveFileName().Match(Path.GetFileName(path)))
            .Where(m => m.Success && m.Groups["variant"].Value == suffix)
            .Select(m => long.Parse(m.Groups["build"].Value, CultureInfo.InvariantCulture))
            .OrderDescending()
            .ToArray();
    }

    /// <summary>Deletes all but the newest <paramref name="keep"/> archives of a variant.</summary>
    public void Prune(int keep, SdeVariant variant = SdeVariant.JsonLines)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(keep);
        foreach (var build in GetCachedBuilds(variant).Skip(keep))
        {
            try
            {
                File.Delete(GetArchivePath(build, variant));
            }
            catch (IOException)
            {
                // Still open by a reader; it will be pruned next time.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    [GeneratedRegex(@"^eve-online-static-data-(?<build>\d+)-(?<variant>[a-z]+)\.zip$")]
    private static partial Regex ArchiveFileName();
}
