namespace NewEdenKit.Sde;

/// <summary>Identifies one published SDE build.</summary>
/// <param name="BuildNumber">The EVE client build number the SDE was exported from.</param>
/// <param name="ReleaseDate">When the build was released.</param>
public sealed record SdeBuildInfo(long BuildNumber, DateTimeOffset ReleaseDate);

/// <summary>The archive formats CCP publishes the SDE in.</summary>
public enum SdeVariant
{
    /// <summary>JSON Lines (one record per line). This is the format <see cref="ISdeReader"/> reads.</summary>
    JsonLines,

    /// <summary>YAML. Available for download only.</summary>
    Yaml,
}

/// <summary>Thrown when SDE data cannot be found or parsed.</summary>
public class SdeException : Exception
{
    /// <summary>Creates a new exception.</summary>
    public SdeException(string message) : base(message)
    {
    }

    /// <summary>Creates a new exception wrapping <paramref name="innerException"/>.</summary>
    public SdeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

internal static class SdeVariantExtensions
{
    public static string ToFileSuffix(this SdeVariant variant) => variant switch
    {
        SdeVariant.JsonLines => "jsonl",
        SdeVariant.Yaml => "yaml",
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    public static string GetArchiveFileName(this SdeVariant variant, long buildNumber) =>
        $"eve-online-static-data-{buildNumber}-{variant.ToFileSuffix()}.zip";
}
