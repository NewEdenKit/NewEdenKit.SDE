namespace NewEdenKit.Sde.Models;

/// <summary>An item type (<c>types.jsonl</c>). <see cref="SdeEntity.Id"/> is the type ID.</summary>
public sealed record ItemType : SdeEntity
{
    /// <summary>Name of the type.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the type.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>The group this type belongs to.</summary>
    public int GroupId { get; init; }

    /// <summary>The market group, when the type is sold on the market.</summary>
    public int? MarketGroupId { get; init; }

    /// <summary>The meta group (Tech I, Tech II, Faction, ...).</summary>
    public int? MetaGroupId { get; init; }

    /// <summary>The meta level.</summary>
    public int? MetaLevel { get; init; }

    /// <summary>The tech level.</summary>
    public int? TechLevel { get; init; }

    /// <summary>The type this is a variation of.</summary>
    public int? VariationParentTypeId { get; init; }

    /// <summary>Whether the type is published in game.</summary>
    public bool Published { get; init; }

    /// <summary>The number of units produced or consumed as one portion.</summary>
    public int PortionSize { get; init; }

    /// <summary>Base price in ISK.</summary>
    public double? BasePrice { get; init; }

    /// <summary>Cargo capacity in m³.</summary>
    public double? Capacity { get; init; }

    /// <summary>Mass in kg.</summary>
    public double? Mass { get; init; }

    /// <summary>Volume in m³.</summary>
    public double? Volume { get; init; }

    /// <summary>Volume in m³ when packaged.</summary>
    public double? PackagedVolume { get; init; }

    /// <summary>Radius in meters.</summary>
    public double? Radius { get; init; }

    /// <summary>The faction this type belongs to.</summary>
    public int? FactionId { get; init; }

    /// <summary>The race this type belongs to.</summary>
    public int? RaceId { get; init; }

    /// <summary>The graphic, see <see cref="Graphic"/>.</summary>
    public int? GraphicId { get; init; }

    /// <summary>The icon, see <see cref="Icon"/>.</summary>
    public int? IconId { get; init; }

    /// <summary>The sound.</summary>
    public int? SoundId { get; init; }

    /// <summary>The ship tree group.</summary>
    public int? ShipTreeGroupId { get; init; }

    /// <summary>Whether the type is a dynamic (mutated) item.</summary>
    public bool? IsDynamicType { get; init; }

    /// <summary>Whether the type can be repackaged.</summary>
    public bool? IsRepackable { get; init; }
}

/// <summary>An item group (<c>groups.jsonl</c>). <see cref="SdeEntity.Id"/> is the group ID.</summary>
public sealed record ItemGroup : SdeEntity
{
    /// <summary>Name of the group.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>The category this group belongs to.</summary>
    public int CategoryId { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>Whether items in the group can be anchored.</summary>
    public bool Anchorable { get; init; }

    /// <summary>Whether items in the group are always anchored.</summary>
    public bool Anchored { get; init; }

    /// <summary>Whether non-singleton items in the group can be fitted.</summary>
    public bool FittableNonSingleton { get; init; }

    /// <summary>Whether the group is published in game.</summary>
    public bool Published { get; init; }

    /// <summary>Whether types in the group use their base price.</summary>
    public bool UseBasePrice { get; init; }
}

/// <summary>An item category (<c>categories.jsonl</c>). <see cref="SdeEntity.Id"/> is the category ID.</summary>
public sealed record ItemCategory : SdeEntity
{
    /// <summary>Name of the category.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>Whether the category is published in game.</summary>
    public bool Published { get; init; }
}

/// <summary>A market group (<c>marketGroups.jsonl</c>). <see cref="SdeEntity.Id"/> is the market group ID.</summary>
public sealed record MarketGroup : SdeEntity
{
    /// <summary>Name of the market group.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the market group.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>The parent market group, or <see langword="null"/> for a root group.</summary>
    public int? ParentGroupId { get; init; }

    /// <summary>Whether types are listed directly in this group.</summary>
    public bool HasTypes { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }
}

/// <summary>A meta group such as Tech II or Faction (<c>metaGroups.jsonl</c>).</summary>
public sealed record MetaGroup : SdeEntity
{
    /// <summary>Name of the meta group.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the meta group.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>The color used for the meta group in the UI.</summary>
    public SdeColor? Color { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>Suffix of the icon overlay (for example <c>t2</c>).</summary>
    public string? IconSuffix { get; init; }
}

/// <summary>The reprocessing output of a type (<c>typeMaterials.jsonl</c>). <see cref="SdeEntity.Id"/> is the type ID.</summary>
public sealed record TypeMaterials : SdeEntity
{
    /// <summary>Materials gained from reprocessing one portion.</summary>
    public IReadOnlyList<TypeMaterial> Materials { get; init; } = [];

    /// <summary>Materials with a randomized quantity.</summary>
    public IReadOnlyList<RandomizedTypeMaterial> RandomizedMaterials { get; init; } = [];
}

/// <summary>A material and quantity.</summary>
public sealed record TypeMaterial : SdeRecord
{
    /// <summary>The material type.</summary>
    public int MaterialTypeId { get; init; }

    /// <summary>The quantity.</summary>
    public int Quantity { get; init; }
}

/// <summary>A material with a randomized quantity.</summary>
public sealed record RandomizedTypeMaterial : SdeRecord
{
    /// <summary>The material type.</summary>
    public int MaterialTypeId { get; init; }

    /// <summary>The minimum quantity.</summary>
    public int QuantityMin { get; init; }

    /// <summary>The maximum quantity.</summary>
    public int QuantityMax { get; init; }
}

/// <summary>An icon (<c>icons.jsonl</c>). <see cref="SdeEntity.Id"/> is the icon ID.</summary>
public sealed record Icon : SdeEntity
{
    /// <summary>Client resource path of the icon (for example <c>res:/ui/texture/icons/7_64_15.png</c>).</summary>
    public string IconFile { get; init; } = string.Empty;
}

/// <summary>A graphic (<c>graphics.jsonl</c>). <see cref="SdeEntity.Id"/> is the graphic ID.</summary>
public sealed record Graphic : SdeEntity
{
    /// <summary>Client resource path of the graphic.</summary>
    public string? GraphicFile { get; init; }

    /// <summary>Client resource folder of the graphic's icons.</summary>
    public string? IconFolder { get; init; }

    /// <summary>Space Object Factory faction name.</summary>
    public string? SofFactionName { get; init; }

    /// <summary>Space Object Factory hull name.</summary>
    public string? SofHullName { get; init; }

    /// <summary>Space Object Factory race name.</summary>
    public string? SofRaceName { get; init; }

    /// <summary>Space Object Factory material set.</summary>
    public int? SofMaterialSetId { get; init; }

    /// <summary>Space Object Factory layout.</summary>
    public IReadOnlyList<string> SofLayout { get; init; } = [];
}
