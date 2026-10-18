using System.Text.Json;
using NewEdenKit.Sde.Models;

namespace NewEdenKit.Sde;

/// <summary>
/// The SDE datasets this library has typed models for. Any other dataset in the archive can be read
/// with <see cref="Raw"/>.
/// </summary>
public static class SdeDatasets
{
    /// <summary><c>types</c>: every item type.</summary>
    public static SdeDataset<ItemType> Types { get; } = new("types");

    /// <summary><c>groups</c>: item groups.</summary>
    public static SdeDataset<ItemGroup> Groups { get; } = new("groups");

    /// <summary><c>categories</c>: item categories.</summary>
    public static SdeDataset<ItemCategory> Categories { get; } = new("categories");

    /// <summary><c>marketGroups</c>: the market tree.</summary>
    public static SdeDataset<MarketGroup> MarketGroups { get; } = new("marketGroups");

    /// <summary><c>metaGroups</c>: Tech I, Tech II, Faction, ...</summary>
    public static SdeDataset<MetaGroup> MetaGroups { get; } = new("metaGroups");

    /// <summary><c>typeMaterials</c>: reprocessing output per type.</summary>
    public static SdeDataset<TypeMaterials> TypeMaterials { get; } = new("typeMaterials");

    /// <summary><c>icons</c>: icon resource paths.</summary>
    public static SdeDataset<Icon> Icons { get; } = new("icons");

    /// <summary><c>graphics</c>: graphic resource paths.</summary>
    public static SdeDataset<Graphic> Graphics { get; } = new("graphics");

    /// <summary><c>typeDogma</c>: attributes and effects per type.</summary>
    public static SdeDataset<TypeDogma> TypeDogma { get; } = new("typeDogma");

    /// <summary><c>dogmaAttributes</c>: attribute definitions.</summary>
    public static SdeDataset<DogmaAttribute> DogmaAttributes { get; } = new("dogmaAttributes");

    /// <summary><c>dogmaAttributeCategories</c>: attribute categories.</summary>
    public static SdeDataset<DogmaAttributeCategory> DogmaAttributeCategories { get; } = new("dogmaAttributeCategories");

    /// <summary><c>dogmaEffects</c>: effect definitions.</summary>
    public static SdeDataset<DogmaEffect> DogmaEffects { get; } = new("dogmaEffects");

    /// <summary><c>dogmaUnits</c>: attribute units.</summary>
    public static SdeDataset<DogmaUnit> DogmaUnits { get; } = new("dogmaUnits");

    /// <summary><c>blueprints</c>: industry activities per blueprint.</summary>
    public static SdeDataset<Blueprint> Blueprints { get; } = new("blueprints");

    /// <summary><c>mapRegions</c>: regions.</summary>
    public static SdeDataset<Region> Regions { get; } = new("mapRegions");

    /// <summary><c>mapConstellations</c>: constellations.</summary>
    public static SdeDataset<Constellation> Constellations { get; } = new("mapConstellations");

    /// <summary><c>mapSolarSystems</c>: solar systems.</summary>
    public static SdeDataset<SolarSystem> SolarSystems { get; } = new("mapSolarSystems");

    /// <summary><c>mapStargates</c>: stargates.</summary>
    public static SdeDataset<Stargate> Stargates { get; } = new("mapStargates");

    /// <summary><c>mapStars</c>: stars.</summary>
    public static SdeDataset<Star> Stars { get; } = new("mapStars");

    /// <summary><c>mapPlanets</c>: planets.</summary>
    public static SdeDataset<Planet> Planets { get; } = new("mapPlanets");

    /// <summary><c>mapMoons</c>: moons. This is the largest dataset (roughly 340,000 records).</summary>
    public static SdeDataset<Moon> Moons { get; } = new("mapMoons");

    /// <summary><c>mapAsteroidBelts</c>: asteroid belts.</summary>
    public static SdeDataset<AsteroidBelt> AsteroidBelts { get; } = new("mapAsteroidBelts");

    /// <summary><c>npcStations</c>: NPC stations.</summary>
    public static SdeDataset<NpcStation> NpcStations { get; } = new("npcStations");

    /// <summary><c>stationOperations</c>: station operations.</summary>
    public static SdeDataset<StationOperation> StationOperations { get; } = new("stationOperations");

    /// <summary><c>stationServices</c>: station services.</summary>
    public static SdeDataset<StationService> StationServices { get; } = new("stationServices");

    /// <summary><c>npcCorporations</c>: NPC corporations.</summary>
    public static SdeDataset<NpcCorporation> NpcCorporations { get; } = new("npcCorporations");

    /// <summary><c>factions</c>: factions.</summary>
    public static SdeDataset<Faction> Factions { get; } = new("factions");

    /// <summary><c>races</c>: races.</summary>
    public static SdeDataset<Race> Races { get; } = new("races");

    /// <summary><c>bloodlines</c>: bloodlines.</summary>
    public static SdeDataset<Bloodline> Bloodlines { get; } = new("bloodlines");

    /// <summary><c>ancestries</c>: ancestries.</summary>
    public static SdeDataset<Ancestry> Ancestries { get; } = new("ancestries");

    /// <summary>Every typed dataset above.</summary>
    public static IReadOnlyList<SdeDataset> All { get; } =
    [
        Types, Groups, Categories, MarketGroups, MetaGroups, TypeMaterials, Icons, Graphics,
        TypeDogma, DogmaAttributes, DogmaAttributeCategories, DogmaEffects, DogmaUnits, Blueprints,
        Regions, Constellations, SolarSystems, Stargates, Stars, Planets, Moons, AsteroidBelts,
        NpcStations, StationOperations, StationServices, NpcCorporations, Factions, Races, Bloodlines, Ancestries,
    ];

    /// <summary>
    /// The item datasets most applications need (types, groups, categories, market and meta groups,
    /// dogma and blueprints), without the large map datasets.
    /// </summary>
    public static IReadOnlyList<SdeDataset> Items { get; } =
    [
        Types, Groups, Categories, MarketGroups, MetaGroups, TypeMaterials, Icons,
        TypeDogma, DogmaAttributes, DogmaAttributeCategories, DogmaEffects, DogmaUnits, Blueprints,
    ];

    /// <summary>
    /// Describes any dataset by name, with each record read as a <see cref="JsonElement"/>.
    /// Use this for datasets without a typed model (for example <c>skins</c> or <c>certificates</c>).
    /// </summary>
    /// <param name="name">The dataset name, which is the file name without <c>.jsonl</c>.</param>
    public static SdeDataset<JsonElement> Raw(string name) => new(name);
}
