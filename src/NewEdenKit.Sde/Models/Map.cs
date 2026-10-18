namespace NewEdenKit.Sde.Models;

/// <summary>A region (<c>mapRegions.jsonl</c>). <see cref="SdeEntity.Id"/> is the region ID.</summary>
public sealed record Region : SdeEntity
{
    /// <summary>Name of the region.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>Description of the region.</summary>
    public LocalizedString? Description { get; init; }

    /// <summary>The constellations in the region.</summary>
    public IReadOnlyList<int> ConstellationIds { get; init; } = [];

    /// <summary>The faction that owns the region.</summary>
    public int? FactionId { get; init; }

    /// <summary>The background nebula.</summary>
    public int NebulaId { get; init; }

    /// <summary>The region's position.</summary>
    public SdePosition Position { get; init; }

    /// <summary>The wormhole class.</summary>
    public int? WormholeClassId { get; init; }
}

/// <summary>A constellation (<c>mapConstellations.jsonl</c>). <see cref="SdeEntity.Id"/> is the constellation ID.</summary>
public sealed record Constellation : SdeEntity
{
    /// <summary>Name of the constellation.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>The region the constellation is in.</summary>
    public int RegionId { get; init; }

    /// <summary>The solar systems in the constellation.</summary>
    public IReadOnlyList<int> SolarSystemIds { get; init; } = [];

    /// <summary>The faction that owns the constellation.</summary>
    public int? FactionId { get; init; }

    /// <summary>The constellation's position.</summary>
    public SdePosition Position { get; init; }

    /// <summary>The wormhole class.</summary>
    public int? WormholeClassId { get; init; }
}

/// <summary>A solar system (<c>mapSolarSystems.jsonl</c>). <see cref="SdeEntity.Id"/> is the solar system ID.</summary>
public sealed record SolarSystem : SdeEntity
{
    /// <summary>Name of the solar system.</summary>
    public LocalizedString Name { get; init; } = LocalizedString.Empty;

    /// <summary>The region the system is in.</summary>
    public int RegionId { get; init; }

    /// <summary>The constellation the system is in.</summary>
    public int ConstellationId { get; init; }

    /// <summary>The true security status, from -1.0 to 1.0.</summary>
    public double SecurityStatus { get; init; }

    /// <summary>The security class.</summary>
    public string? SecurityClass { get; init; }

    /// <summary>The faction that owns the system.</summary>
    public int? FactionId { get; init; }

    /// <summary>The system's star.</summary>
    public int? StarId { get; init; }

    /// <summary>The planets in the system.</summary>
    public IReadOnlyList<int> PlanetIds { get; init; } = [];

    /// <summary>The stargates in the system.</summary>
    public IReadOnlyList<int> StargateIds { get; init; } = [];

    /// <summary>The system's position.</summary>
    public SdePosition Position { get; init; }

    /// <summary>The system's position on the 2D map.</summary>
    public SdePosition2D? Position2D { get; init; }

    /// <summary>Radius of the system in meters.</summary>
    public double Radius { get; init; }

    /// <summary>Luminosity of the system's star.</summary>
    public double? Luminosity { get; init; }

    /// <summary>The wormhole class.</summary>
    public int? WormholeClassId { get; init; }

    /// <summary>Visual effect applied to the system.</summary>
    public string? VisualEffect { get; init; }

    /// <summary>Whether the system borders another region.</summary>
    public bool Border { get; init; }

    /// <summary>Whether the system is a corridor system.</summary>
    public bool Corridor { get; init; }

    /// <summary>Whether the system is a fringe system.</summary>
    public bool Fringe { get; init; }

    /// <summary>Whether the system is a hub system.</summary>
    public bool Hub { get; init; }

    /// <summary>Whether the system borders another faction.</summary>
    public bool International { get; init; }

    /// <summary>Whether the system borders another constellation.</summary>
    public bool Regional { get; init; }

    /// <summary>Categories that cannot be anchored in the system.</summary>
    public IReadOnlyList<int> DisallowedAnchorCategories { get; init; } = [];

    /// <summary>Groups that cannot be anchored in the system.</summary>
    public IReadOnlyList<int> DisallowedAnchorGroups { get; init; } = [];
}

/// <summary>A stargate (<c>mapStargates.jsonl</c>). <see cref="SdeEntity.Id"/> is the stargate ID.</summary>
public sealed record Stargate : SdeEntity
{
    /// <summary>The system the stargate is in.</summary>
    public int SolarSystemId { get; init; }

    /// <summary>The stargate's type.</summary>
    public int TypeId { get; init; }

    /// <summary>Where the stargate leads.</summary>
    public StargateDestination Destination { get; init; }

    /// <summary>The stargate's position in its system.</summary>
    public SdePosition Position { get; init; }
}

/// <summary>The far end of a stargate jump.</summary>
public readonly record struct StargateDestination(int SolarSystemId, int StargateId);

/// <summary>A star (<c>mapStars.jsonl</c>). <see cref="SdeEntity.Id"/> is the star's item ID.</summary>
public sealed record Star : SdeEntity
{
    /// <summary>The system the star is in.</summary>
    public int SolarSystemId { get; init; }

    /// <summary>The star's type.</summary>
    public int TypeId { get; init; }

    /// <summary>Radius in meters.</summary>
    public double Radius { get; init; }

    /// <summary>Physical statistics of the star.</summary>
    public StarStatistics? Statistics { get; init; }
}

/// <summary>Physical statistics of a star.</summary>
public sealed record StarStatistics : SdeRecord
{
    /// <summary>Age.</summary>
    public double? Age { get; init; }

    /// <summary>Life.</summary>
    public double? Life { get; init; }

    /// <summary>Luminosity.</summary>
    public double? Luminosity { get; init; }

    /// <summary>Spectral class.</summary>
    public string? SpectralClass { get; init; }

    /// <summary>Temperature.</summary>
    public double? Temperature { get; init; }
}

/// <summary>Physical statistics of a planet, moon or asteroid belt.</summary>
public sealed record CelestialStatistics : SdeRecord
{
    /// <summary>Density.</summary>
    public double? Density { get; init; }

    /// <summary>Orbital eccentricity.</summary>
    public double? Eccentricity { get; init; }

    /// <summary>Escape velocity.</summary>
    public double? EscapeVelocity { get; init; }

    /// <summary>Whether the body is tidally locked.</summary>
    public bool? Locked { get; init; }

    /// <summary>Dust mass.</summary>
    public double? MassDust { get; init; }

    /// <summary>Gas mass.</summary>
    public double? MassGas { get; init; }

    /// <summary>Orbital period.</summary>
    public double? OrbitPeriod { get; init; }

    /// <summary>Orbital radius.</summary>
    public double? OrbitRadius { get; init; }

    /// <summary>Surface pressure.</summary>
    public double? Pressure { get; init; }

    /// <summary>Rotation rate.</summary>
    public double? RotationRate { get; init; }

    /// <summary>Spectral class.</summary>
    public string? SpectralClass { get; init; }

    /// <summary>Surface gravity.</summary>
    public double? SurfaceGravity { get; init; }

    /// <summary>Temperature.</summary>
    public double? Temperature { get; init; }
}

/// <summary>Rendering attributes of a planet or moon.</summary>
public sealed record CelestialAttributes : SdeRecord
{
    /// <summary>First height map.</summary>
    public int? HeightMap1 { get; init; }

    /// <summary>Second height map.</summary>
    public int? HeightMap2 { get; init; }

    /// <summary>Whether the planet is populated.</summary>
    public bool? Population { get; init; }

    /// <summary>Shader preset.</summary>
    public int? ShaderPreset { get; init; }
}

/// <summary>A planet (<c>mapPlanets.jsonl</c>). <see cref="SdeEntity.Id"/> is the planet's item ID.</summary>
public sealed record Planet : SdeEntity
{
    /// <summary>The system the planet is in.</summary>
    public int SolarSystemId { get; init; }

    /// <summary>The planet's type.</summary>
    public int TypeId { get; init; }

    /// <summary>The object the planet orbits.</summary>
    public int OrbitId { get; init; }

    /// <summary>Position of the planet in its system's celestial order.</summary>
    public int CelestialIndex { get; init; }

    /// <summary>Unique name, for planets with one.</summary>
    public LocalizedString? UniqueName { get; init; }

    /// <summary>The planet's position in its system.</summary>
    public SdePosition Position { get; init; }

    /// <summary>Radius in meters.</summary>
    public double Radius { get; init; }

    /// <summary>The moons orbiting the planet.</summary>
    public IReadOnlyList<int> MoonIds { get; init; } = [];

    /// <summary>The asteroid belts orbiting the planet.</summary>
    public IReadOnlyList<int> AsteroidBeltIds { get; init; } = [];

    /// <summary>NPC stations orbiting the planet.</summary>
    public IReadOnlyList<int> NpcStationIds { get; init; } = [];

    /// <summary>Rendering attributes.</summary>
    public CelestialAttributes? Attributes { get; init; }

    /// <summary>Physical statistics.</summary>
    public CelestialStatistics? Statistics { get; init; }
}

/// <summary>A moon (<c>mapMoons.jsonl</c>). <see cref="SdeEntity.Id"/> is the moon's item ID.</summary>
public sealed record Moon : SdeEntity
{
    /// <summary>The system the moon is in.</summary>
    public int SolarSystemId { get; init; }

    /// <summary>The moon's type.</summary>
    public int TypeId { get; init; }

    /// <summary>The planet the moon orbits.</summary>
    public int OrbitId { get; init; }

    /// <summary>Position of the parent planet in its system's celestial order.</summary>
    public int CelestialIndex { get; init; }

    /// <summary>Position of the moon around its planet.</summary>
    public int OrbitIndex { get; init; }

    /// <summary>Unique name, for moons with one.</summary>
    public LocalizedString? UniqueName { get; init; }

    /// <summary>The moon's position in its system.</summary>
    public SdePosition Position { get; init; }

    /// <summary>Radius in meters.</summary>
    public double Radius { get; init; }

    /// <summary>NPC stations orbiting the moon.</summary>
    public IReadOnlyList<int> NpcStationIds { get; init; } = [];

    /// <summary>Rendering attributes.</summary>
    public CelestialAttributes? Attributes { get; init; }

    /// <summary>Physical statistics.</summary>
    public CelestialStatistics? Statistics { get; init; }
}

/// <summary>An asteroid belt (<c>mapAsteroidBelts.jsonl</c>). <see cref="SdeEntity.Id"/> is the belt's item ID.</summary>
public sealed record AsteroidBelt : SdeEntity
{
    /// <summary>The system the belt is in.</summary>
    public int SolarSystemId { get; init; }

    /// <summary>The belt's type.</summary>
    public int TypeId { get; init; }

    /// <summary>The planet the belt orbits.</summary>
    public int OrbitId { get; init; }

    /// <summary>Position of the parent planet in its system's celestial order.</summary>
    public int CelestialIndex { get; init; }

    /// <summary>Position of the belt around its planet.</summary>
    public int OrbitIndex { get; init; }

    /// <summary>Unique name, for belts with one.</summary>
    public LocalizedString? UniqueName { get; init; }

    /// <summary>The belt's position in its system.</summary>
    public SdePosition Position { get; init; }

    /// <summary>Radius in meters.</summary>
    public double? Radius { get; init; }

    /// <summary>Physical statistics.</summary>
    public CelestialStatistics? Statistics { get; init; }
}

/// <summary>An NPC station (<c>npcStations.jsonl</c>). <see cref="SdeEntity.Id"/> is the station ID.</summary>
public sealed record NpcStation : SdeEntity
{
    /// <summary>The system the station is in.</summary>
    public int SolarSystemId { get; init; }

    /// <summary>The station's type.</summary>
    public int TypeId { get; init; }

    /// <summary>The owning corporation.</summary>
    public int OwnerId { get; init; }

    /// <summary>The station operation, see <see cref="StationOperation"/>. Combined with the owner it forms the station's name.</summary>
    public int OperationId { get; init; }

    /// <summary>Whether the operation name is part of the station name.</summary>
    public bool UseOperationName { get; init; }

    /// <summary>The celestial the station orbits.</summary>
    public int OrbitId { get; init; }

    /// <summary>Position of the orbited planet in its system's celestial order.</summary>
    public int? CelestialIndex { get; init; }

    /// <summary>Position of the orbited moon around its planet.</summary>
    public int? OrbitIndex { get; init; }

    /// <summary>The station's position in its system.</summary>
    public SdePosition Position { get; init; }

    /// <summary>Base reprocessing efficiency.</summary>
    public double ReprocessingEfficiency { get; init; }

    /// <summary>Share of reprocessing output the station keeps.</summary>
    public double ReprocessingStationsTake { get; init; }

    /// <summary>Hangar flag for reprocessing output.</summary>
    public int ReprocessingHangarFlag { get; init; }
}
