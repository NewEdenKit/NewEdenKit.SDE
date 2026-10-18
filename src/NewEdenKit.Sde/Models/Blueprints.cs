using System.Text.Json.Serialization;

namespace NewEdenKit.Sde.Models;

/// <summary>A blueprint (<c>blueprints.jsonl</c>). <see cref="SdeEntity.Id"/> is the blueprint type ID.</summary>
public sealed record Blueprint : SdeEntity
{
    /// <summary>The blueprint's type ID.</summary>
    public int BlueprintTypeId { get; init; }

    /// <summary>Maximum runs on a single copy.</summary>
    public int MaxProductionLimit { get; init; }

    /// <summary>The industry activities this blueprint supports.</summary>
    public BlueprintActivities Activities { get; init; } = new();
}

/// <summary>The industry activities of a blueprint. Activities the blueprint does not support are <see langword="null"/>.</summary>
public sealed record BlueprintActivities : SdeRecord
{
    /// <summary>Manufacturing.</summary>
    public BlueprintActivity? Manufacturing { get; init; }

    /// <summary>Reactions.</summary>
    public BlueprintActivity? Reaction { get; init; }

    /// <summary>Invention.</summary>
    public BlueprintActivity? Invention { get; init; }

    /// <summary>Copying.</summary>
    public BlueprintActivity? Copying { get; init; }

    /// <summary>Material efficiency research.</summary>
    [JsonPropertyName("research_material")]
    public BlueprintActivity? ResearchMaterial { get; init; }

    /// <summary>Time efficiency research.</summary>
    [JsonPropertyName("research_time")]
    public BlueprintActivity? ResearchTime { get; init; }
}

/// <summary>One industry activity of a blueprint.</summary>
public sealed record BlueprintActivity : SdeRecord
{
    /// <summary>Base time in seconds.</summary>
    public int Time { get; init; }

    /// <summary>Materials consumed.</summary>
    public IReadOnlyList<BlueprintMaterial> Materials { get; init; } = [];

    /// <summary>Products created.</summary>
    public IReadOnlyList<BlueprintProduct> Products { get; init; } = [];

    /// <summary>Skills required.</summary>
    public IReadOnlyList<BlueprintSkill> Skills { get; init; } = [];
}

/// <summary>A material consumed by a blueprint activity.</summary>
public sealed record BlueprintMaterial : SdeRecord
{
    /// <summary>The material type.</summary>
    public int TypeId { get; init; }

    /// <summary>The quantity.</summary>
    public int Quantity { get; init; }
}

/// <summary>A product of a blueprint activity.</summary>
public sealed record BlueprintProduct : SdeRecord
{
    /// <summary>The product type.</summary>
    public int TypeId { get; init; }

    /// <summary>The quantity.</summary>
    public int Quantity { get; init; }

    /// <summary>Base success chance, for invention.</summary>
    public double? Probability { get; init; }
}

/// <summary>A skill required by a blueprint activity.</summary>
public sealed record BlueprintSkill : SdeRecord
{
    /// <summary>The skill type.</summary>
    public int TypeId { get; init; }

    /// <summary>The required level.</summary>
    public int Level { get; init; }
}
