using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewEdenKit.Sde;

/// <summary>
/// Base for every typed SDE object. Fields the model does not know about (for example fields CCP
/// adds in a later build) are kept in <see cref="AdditionalProperties"/> instead of being dropped.
/// </summary>
public abstract record SdeRecord
{
    /// <summary>Fields present in the data but not mapped to a property.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

/// <summary>A top-level SDE record identified by an integer key.</summary>
public abstract record SdeEntity : SdeRecord
{
    /// <summary>The record's key (type ID, group ID, solar system ID, ...).</summary>
    [JsonPropertyName("_key")]
    public int Id { get; init; }
}
