using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using NewEdenKit.Sde.Serialization;

namespace NewEdenKit.Sde;

/// <summary>
/// A piece of text in several languages, keyed by language code (<c>en</c>, <c>de</c>, <c>fr</c>, ...).
/// </summary>
[JsonConverter(typeof(LocalizedStringConverter))]
public sealed class LocalizedString : IReadOnlyDictionary<string, string>, IEquatable<LocalizedString>
{
    /// <summary>The language the SDE always provides and that lookups fall back to.</summary>
    public const string DefaultLanguage = "en";

    private readonly IReadOnlyDictionary<string, string> _values;

    /// <summary>Creates a localized string from language/text pairs.</summary>
    public LocalizedString(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values;
    }

    /// <summary>A localized string with no translations.</summary>
    public static LocalizedString Empty { get; } = new(new Dictionary<string, string>());

    /// <summary>English text, or <see langword="null"/> when missing.</summary>
    public string? En => Get("en");

    /// <summary>German text, or <see langword="null"/> when missing.</summary>
    public string? De => Get("de");

    /// <summary>Spanish text, or <see langword="null"/> when missing.</summary>
    public string? Es => Get("es");

    /// <summary>French text, or <see langword="null"/> when missing.</summary>
    public string? Fr => Get("fr");

    /// <summary>Japanese text, or <see langword="null"/> when missing.</summary>
    public string? Ja => Get("ja");

    /// <summary>Korean text, or <see langword="null"/> when missing.</summary>
    public string? Ko => Get("ko");

    /// <summary>Russian text, or <see langword="null"/> when missing.</summary>
    public string? Ru => Get("ru");

    /// <summary>Chinese text, or <see langword="null"/> when missing.</summary>
    public string? Zh => Get("zh");

    /// <summary>Returns the text for <paramref name="language"/>, or <see langword="null"/> when missing.</summary>
    public string? Get(string language) => _values.TryGetValue(language, out var value) ? value : null;

    /// <summary>
    /// Returns the text for <paramref name="language"/>, falling back to <paramref name="fallbackLanguage"/>,
    /// then to any available translation, then to an empty string.
    /// </summary>
    public string GetOrDefault(string language, string fallbackLanguage = DefaultLanguage) =>
        Get(language) ?? Get(fallbackLanguage) ?? _values.Values.FirstOrDefault() ?? string.Empty;

    /// <inheritdoc />
    public string this[string key] => _values[key];

    /// <inheritdoc />
    public IEnumerable<string> Keys => _values.Keys;

    /// <inheritdoc />
    public IEnumerable<string> Values => _values.Values;

    /// <inheritdoc />
    public int Count => _values.Count;

    /// <inheritdoc />
    public bool ContainsKey(string key) => _values.ContainsKey(key);

    /// <inheritdoc />
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value) => _values.TryGetValue(key, out value);

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    public bool Equals(LocalizedString? other) =>
        other is not null &&
        (ReferenceEquals(this, other) ||
         (Count == other.Count && _values.All(kv => other.TryGetValue(kv.Key, out var v) && v == kv.Value)));

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as LocalizedString);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(En ?? string.Empty);

    /// <summary>Returns the English text (or the first available translation).</summary>
    public override string ToString() => GetOrDefault(DefaultLanguage);
}
