using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewEdenKit.Sde.Serialization;

/// <summary>JSON settings used to read and write SDE records.</summary>
public static class SdeJson
{
    /// <summary>
    /// The shared, read-only options used by the readers. Property names match case-insensitively,
    /// so a C# property <c>GroupId</c> binds to the SDE field <c>groupID</c>.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    /// <summary>
    /// The shared, read-only options used to write records to files: like <see cref="Options"/>, but
    /// null values are left out and non-ASCII text is written as-is to keep files compact.
    /// </summary>
    public static JsonSerializerOptions WriteOptions { get; } = new(CreateOptions())
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Creates a new, mutable copy of the default SDE options for customization.</summary>
    public static JsonSerializerOptions CreateOptions() => new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.Strict,
    };

    static SdeJson()
    {
        Options.MakeReadOnly(populateMissingResolver: true);
        WriteOptions.MakeReadOnly(populateMissingResolver: true);
    }
}
