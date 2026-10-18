using System.Text.Json.Serialization;
using NewEdenKit.Sde.Serialization;

namespace NewEdenKit.Sde.Models;

/// <summary>A faction (<c>factions.jsonl</c>). <see cref="SdeEntity.Id"/> is the faction ID.</summary>
public sealed record Faction : SdeEntity
{
    /// <summary>Name of the faction.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the faction.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>Short description of the faction.</summary>
    public LocalizedString? ShortDescription { get; init; }

    /// <summary>The faction's executor corporation.</summary>
    public int? CorporationId { get; init; }

    /// <summary>The faction's militia corporation.</summary>
    public int? MilitiaCorporationId { get; init; }

    /// <summary>The faction's home system.</summary>
    public int SolarSystemId { get; init; }

    /// <summary>The races in the faction.</summary>
    public IReadOnlyList<int> MemberRaces { get; init; } = [];

    /// <summary>The icon.</summary>
    public int IconId { get; init; }

    /// <summary>Client resource path of the flat logo.</summary>
    public string? FlatLogo { get; init; }

    /// <summary>Client resource path of the flat logo with name.</summary>
    public string? FlatLogoWithName { get; init; }

    /// <summary>Size factor.</summary>
    public double SizeFactor { get; init; }

    /// <summary>Whether the faction name is unique.</summary>
    public bool UniqueName { get; init; }
}

/// <summary>A race (<c>races.jsonl</c>). <see cref="SdeEntity.Id"/> is the race ID.</summary>
public sealed record Race : SdeEntity
{
    /// <summary>Name of the race.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the race.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>The race's starter ship.</summary>
    public int? ShipTypeId { get; init; }

    /// <summary>Starting skill levels, keyed by skill type ID.</summary>
    [JsonConverter(typeof(KeyedValueListConverter<int>))]
    public IReadOnlyDictionary<int, int> Skills { get; init; } = new Dictionary<int, int>();
}

/// <summary>A bloodline (<c>bloodlines.jsonl</c>). <see cref="SdeEntity.Id"/> is the bloodline ID.</summary>
public sealed record Bloodline : SdeEntity
{
    /// <summary>Name of the bloodline.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the bloodline.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>The race of the bloodline.</summary>
    public int RaceId { get; init; }

    /// <summary>The bloodline's starter corporation.</summary>
    public int CorporationId { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>Starting charisma.</summary>
    public int Charisma { get; init; }

    /// <summary>Starting intelligence.</summary>
    public int Intelligence { get; init; }

    /// <summary>Starting memory.</summary>
    public int Memory { get; init; }

    /// <summary>Starting perception.</summary>
    public int Perception { get; init; }

    /// <summary>Starting willpower.</summary>
    public int Willpower { get; init; }
}

/// <summary>An ancestry (<c>ancestries.jsonl</c>). <see cref="SdeEntity.Id"/> is the ancestry ID.</summary>
public sealed record Ancestry : SdeEntity
{
    /// <summary>Name of the ancestry.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the ancestry.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>Short description (English only).</summary>
    public string? ShortDescription { get; init; }

    /// <summary>The bloodline of the ancestry.</summary>
    public int BloodlineId { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>Charisma bonus.</summary>
    public int Charisma { get; init; }

    /// <summary>Intelligence bonus.</summary>
    public int Intelligence { get; init; }

    /// <summary>Memory bonus.</summary>
    public int Memory { get; init; }

    /// <summary>Perception bonus.</summary>
    public int Perception { get; init; }

    /// <summary>Willpower bonus.</summary>
    public int Willpower { get; init; }
}

/// <summary>An NPC corporation (<c>npcCorporations.jsonl</c>). <see cref="SdeEntity.Id"/> is the corporation ID.</summary>
public sealed record NpcCorporation : SdeEntity
{
    /// <summary>Name of the corporation.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the corporation.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>The corporation's ticker.</summary>
    public string TickerName { get; init; } = string.Empty;

    /// <summary>The faction the corporation belongs to.</summary>
    public int? FactionId { get; init; }

    /// <summary>The corporation's race.</summary>
    public int? RaceId { get; init; }

    /// <summary>The CEO character.</summary>
    public int? CeoId { get; init; }

    /// <summary>The headquarters station.</summary>
    public int? StationId { get; init; }

    /// <summary>The headquarters system.</summary>
    public int? SolarSystemId { get; init; }

    /// <summary>The icon.</summary>
    public int? IconId { get; init; }

    /// <summary>The corporation's friend corporation.</summary>
    public int? FriendId { get; init; }

    /// <summary>The corporation's enemy corporation.</summary>
    public int? EnemyId { get; init; }

    /// <summary>Main corporation activity.</summary>
    public int? MainActivityId { get; init; }

    /// <summary>Secondary corporation activity.</summary>
    public int? SecondaryActivityId { get; init; }

    /// <summary>Races allowed to join.</summary>
    public IReadOnlyList<int> AllowedMemberRaces { get; init; } = [];

    /// <summary>The corporation's agent divisions.</summary>
    public IReadOnlyList<NpcCorporationDivision> Divisions { get; init; } = [];

    /// <summary>Loyalty point store offer tables.</summary>
    public IReadOnlyList<int> LpOfferTables { get; init; } = [];

    /// <summary>Types the corporation trades, with their price factor, keyed by type ID.</summary>
    [JsonConverter(typeof(KeyedValueListConverter<double>))]
    public IReadOnlyDictionary<int, double> CorporationTrades { get; init; } = new Dictionary<int, double>();

    /// <summary>Exchange rates, keyed by corporation ID.</summary>
    [JsonConverter(typeof(KeyedValueListConverter<double>))]
    public IReadOnlyDictionary<int, double> ExchangeRates { get; init; } = new Dictionary<int, double>();

    /// <summary>Shares held by investor corporations, keyed by corporation ID.</summary>
    [JsonConverter(typeof(KeyedValueListConverter<int>))]
    public IReadOnlyDictionary<int, int> Investors { get; init; } = new Dictionary<int, int>();

    /// <summary>Extent code.</summary>
    public string? Extent { get; init; }

    /// <summary>Size code.</summary>
    public string? Size { get; init; }

    /// <summary>Size factor.</summary>
    public double? SizeFactor { get; init; }

    /// <summary>Initial share price.</summary>
    public long InitialPrice { get; init; }

    /// <summary>Number of shares.</summary>
    public long Shares { get; init; }

    /// <summary>Member limit.</summary>
    public int MemberLimit { get; init; }

    /// <summary>Minimum security status to join.</summary>
    public double MinSecurity { get; init; }

    /// <summary>Minimum standing to join.</summary>
    public double MinimumJoinStanding { get; init; }

    /// <summary>Tax rate.</summary>
    public double TaxRate { get; init; }

    /// <summary>Whether the corporation has been deleted.</summary>
    public bool Deleted { get; init; }

    /// <summary>Whether the corporation has a player personnel manager.</summary>
    public bool HasPlayerPersonnelManager { get; init; }

    /// <summary>Whether members are sent a message when leaving.</summary>
    public bool SendCharTerminationMessage { get; init; }

    /// <summary>Whether the corporation name is unique.</summary>
    public bool UniqueName { get; init; }
}

/// <summary>An agent division of an NPC corporation.</summary>
public sealed record NpcCorporationDivision : SdeRecord
{
    /// <summary>The division, see <c>npcCorporationDivisions.jsonl</c>.</summary>
    [JsonPropertyName("_key")]
    public int DivisionId { get; init; }

    /// <summary>The division number.</summary>
    public int DivisionNumber { get; init; }

    /// <summary>The division leader character.</summary>
    public int LeaderId { get; init; }

    /// <summary>Size of the division.</summary>
    public int Size { get; init; }
}

/// <summary>A station operation (<c>stationOperations.jsonl</c>). <see cref="SdeEntity.Id"/> is the operation ID.</summary>
public sealed record StationOperation : SdeEntity
{
    /// <summary>Name of the operation (used in station names).</summary>
    public LocalizedString OperationName { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the operation.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>The corporation activity.</summary>
    public int ActivityId { get; init; }

    /// <summary>The station services offered, see <see cref="StationService"/>.</summary>
    public IReadOnlyList<int> Services { get; init; } = [];

    /// <summary>Station type used per race, keyed by race ID.</summary>
    [JsonConverter(typeof(KeyedValueListConverter<int>))]
    public IReadOnlyDictionary<int, int> StationTypes { get; init; } = new Dictionary<int, int>();

    /// <summary>Weight in border systems.</summary>
    public double Border { get; init; }

    /// <summary>Weight in corridor systems.</summary>
    public double Corridor { get; init; }

    /// <summary>Weight in fringe systems.</summary>
    public double Fringe { get; init; }

    /// <summary>Weight in hub systems.</summary>
    public double Hub { get; init; }

    /// <summary>Ratio.</summary>
    public double Ratio { get; init; }

    /// <summary>Manufacturing factor.</summary>
    public double ManufacturingFactor { get; init; }

    /// <summary>Research factor.</summary>
    public double ResearchFactor { get; init; }
}

/// <summary>A station service (<c>stationServices.jsonl</c>). <see cref="SdeEntity.Id"/> is the service ID.</summary>
public sealed record StationService : SdeEntity
{
    /// <summary>Name of the service.</summary>
    public LocalizedString ServiceName { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the service.</summary>
    public LocalizedString? Description { get; init; }
}
