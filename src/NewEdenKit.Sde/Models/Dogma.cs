using System.Diagnostics.CodeAnalysis;

namespace NewEdenKit.Sde.Models;

/// <summary>The dogma attributes and effects of a type (<c>typeDogma.jsonl</c>). <see cref="SdeEntity.Id"/> is the type ID.</summary>
public sealed record TypeDogma : SdeEntity
{
    /// <summary>Attribute values of the type.</summary>
    public IReadOnlyList<TypeDogmaAttribute> DogmaAttributes { get; init; } = [];

    /// <summary>Effects of the type.</summary>
    public IReadOnlyList<TypeDogmaEffect> DogmaEffects { get; init; } = [];
}

/// <summary>An attribute value on a type.</summary>
[SuppressMessage("Naming", "CA1711", Justification = "Dogma attribute is the EVE term; this is not a .NET attribute.")]
public sealed record TypeDogmaAttribute : SdeRecord
{
    /// <summary>The attribute, see <see cref="DogmaAttribute"/>.</summary>
    public int AttributeId { get; init; }

    /// <summary>The value.</summary>
    public double Value { get; init; }
}

/// <summary>An effect on a type.</summary>
public sealed record TypeDogmaEffect : SdeRecord
{
    /// <summary>The effect, see <see cref="DogmaEffect"/>.</summary>
    public int EffectId { get; init; }

    /// <summary>Whether this is the type's default effect.</summary>
    public bool IsDefault { get; init; }
}

/// <summary>A dogma attribute definition (<c>dogmaAttributes.jsonl</c>). <see cref="SdeEntity.Id"/> is the attribute ID.</summary>
[SuppressMessage("Naming", "CA1711", Justification = "Dogma attribute is the EVE term; this is not a .NET attribute.")]
public sealed record DogmaAttribute : SdeEntity
{
    /// <summary>Internal name of the attribute.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Internal description (English only).</summary>
    public string? Description { get; init; }

    /// <summary>Name shown in the UI.</summary>
    public LocalizedString? DisplayName { get; init; }

    /// <summary>Tooltip title shown in the UI.</summary>
    public LocalizedString? TooltipTitle { get; init; }

    /// <summary>Tooltip description shown in the UI.</summary>
    public LocalizedString? TooltipDescription { get; init; }

    /// <summary>The attribute category, see <see cref="DogmaAttributeCategory"/>.</summary>
    public int? AttributeCategoryId { get; init; }

    /// <summary>The unit, see <see cref="DogmaUnit"/>.</summary>
    public int? UnitId { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>The data type code.</summary>
    public int DataType { get; init; }

    /// <summary>Value used when a type does not define the attribute.</summary>
    public double DefaultValue { get; init; }

    /// <summary>Attribute that caps this attribute's value.</summary>
    public int? MaxAttributeId { get; init; }

    /// <summary>Attribute that is the floor of this attribute's value.</summary>
    public int? MinAttributeId { get; init; }

    /// <summary>Attribute holding the charge recharge time.</summary>
    public int? ChargeRechargeTimeId { get; init; }

    /// <summary>Whether to show the attribute when its value is zero.</summary>
    public bool DisplayWhenZero { get; init; }

    /// <summary>Whether a higher value is better.</summary>
    public bool HighIsGood { get; init; }

    /// <summary>Whether the attribute is published in game.</summary>
    public bool Published { get; init; }

    /// <summary>Whether modifiers on this attribute stack without penalty.</summary>
    public bool Stackable { get; init; }
}

/// <summary>A dogma attribute category (<c>dogmaAttributeCategories.jsonl</c>).</summary>
public sealed record DogmaAttributeCategory : SdeEntity
{
    /// <summary>Name of the category.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Description of the category.</summary>
    public string? Description { get; init; }
}

/// <summary>A dogma unit such as meters or seconds (<c>dogmaUnits.jsonl</c>).</summary>
public sealed record DogmaUnit : SdeEntity
{
    /// <summary>Internal name of the unit.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Name or symbol shown in the UI.</summary>
    public LocalizedString? DisplayName { get; init; }

    /// <summary>Description of the unit.</summary>
    public LocalizedString? Description { get; init; }
}

/// <summary>A dogma effect definition (<c>dogmaEffects.jsonl</c>). <see cref="SdeEntity.Id"/> is the effect ID.</summary>
public sealed record DogmaEffect : SdeEntity
{
    /// <summary>Internal name of the effect.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Name shown in the UI.</summary>
    public LocalizedString? DisplayName { get; init; }

    /// <summary>Description of the effect.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>Effect GUID.</summary>
    [SuppressMessage("Naming", "CA1720", Justification = "Matches the SDE field name.")]
    public string? Guid { get; init; }

    /// <summary>The effect category.</summary>
    public int EffectCategoryId { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>The modifiers the effect applies.</summary>
    public IReadOnlyList<DogmaEffectModifier> ModifierInfo { get; init; } = [];

    /// <summary>Attribute that drives the effect's capacitor use.</summary>
    public int? DischargeAttributeId { get; init; }

    /// <summary>Attribute that drives the effect's cycle time.</summary>
    public int? DurationAttributeId { get; init; }

    /// <summary>Attribute that drives the effect's optimal range.</summary>
    public int? RangeAttributeId { get; init; }

    /// <summary>Attribute that drives the effect's falloff.</summary>
    public int? FalloffAttributeId { get; init; }

    /// <summary>Attribute that drives the effect's tracking speed.</summary>
    public int? TrackingSpeedAttributeId { get; init; }

    /// <summary>Attribute that resists the effect.</summary>
    public int? ResistanceAttributeId { get; init; }

    /// <summary>Attribute holding the chance of the effect being used when fitted.</summary>
    public int? FittingUsageChanceAttributeId { get; init; }

    /// <summary>Attribute holding the chance an NPC activates the effect.</summary>
    public int? NpcActivationChanceAttributeId { get; init; }

    /// <summary>Attribute holding the chance an NPC uses the effect.</summary>
    public int? NpcUsageChanceAttributeId { get; init; }

    /// <summary>Distribution code.</summary>
    public int? Distribution { get; init; }

    /// <summary>Whether the effect may not auto-repeat.</summary>
    public bool DisallowAutoRepeat { get; init; }

    /// <summary>Whether the effect has an electronic chance.</summary>
    public bool ElectronicChance { get; init; }

    /// <summary>Whether the effect is assistance.</summary>
    public bool IsAssistance { get; init; }

    /// <summary>Whether the effect is offensive.</summary>
    public bool IsOffensive { get; init; }

    /// <summary>Whether the effect is safe to use while warping.</summary>
    public bool IsWarpSafe { get; init; }

    /// <summary>Whether the effect has a propulsion chance.</summary>
    public bool PropulsionChance { get; init; }

    /// <summary>Whether the effect is published.</summary>
    public bool Published { get; init; }

    /// <summary>Whether the effect has a range chance.</summary>
    public bool RangeChance { get; init; }
}

/// <summary>One modifier applied by a <see cref="DogmaEffect"/>.</summary>
public sealed record DogmaEffectModifier : SdeRecord
{
    /// <summary>What the modifier applies to (for example <c>shipID</c>, <c>charID</c>, <c>itemID</c>).</summary>
    public string? Domain { get; init; }

    /// <summary>How targets are selected (for example <c>ItemModifier</c>, <c>LocationGroupModifier</c>).</summary>
    public string? Func { get; init; }

    /// <summary>The attribute being modified.</summary>
    public int? ModifiedAttributeId { get; init; }

    /// <summary>The attribute providing the modification value.</summary>
    public int? ModifyingAttributeId { get; init; }

    /// <summary>The operation code.</summary>
    public int? Operation { get; init; }

    /// <summary>The group filter, for group modifiers.</summary>
    public int? GroupId { get; init; }

    /// <summary>The required skill filter, for skill modifiers.</summary>
    public int? SkillTypeId { get; init; }

    /// <summary>Effect referenced by the modifier.</summary>
    public int? EffectId { get; init; }
}
