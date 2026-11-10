using Il2CppReloaded.Gameplay;
using UnityEngine;

namespace PlantLibrary;

/// <summary>Serialized identity. NativeType always remains a real game seed.</summary>
public readonly record struct PlantIdentity(SeedType NativeType, SeedType Marker = SeedType.None);
public enum UnlockPolicy { Never, Always, Explicit }
public sealed record EntryText(string Name, string Info, string Flavor = "", string Notes = "", string[]? Recipes = null) {
    public string[] Recipes { get; init; } = Recipes ?? Array.Empty<string>();
}
public sealed record Entry(int SourceId, PlantIdentity Plant, EntryText English, EntryText German, string Portrait);
public sealed record Ingredient(int Id, string English, string German, string Portrait);

/// <summary>A mod owns its descriptions, embedded resources and optional related-result discovery.</summary>
public sealed class ModCatalog {
    public string Id { get; }
    public string Name { get; }
    public Func<string, Stream> Resource { get; }
    public Dictionary<PlantIdentity, Ingredient> Ingredients { get; } = new();
    public Func<PlantIdentity, IEnumerable<(PlantIdentity Result, PlantIdentity Partner)>>? Related { get; init; }
    public Func<bool, string> CostNotes { get; init; } = _ => "";
    internal Entry[] Entries => Plants.All.Where(p => p.Mod == this).Select(p => p.Entry).ToArray();
    public ModCatalog(string id, string name, Func<string, Stream> resource) {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) throw new ArgumentException("A mod needs an ID and name.");
        Id = id; Name = name; Resource = resource ?? throw new ArgumentNullException(nameof(resource));
    }
}

public sealed class PlantDefinition {
    public ModCatalog Mod { get; }
    public string Id { get; }
    public Entry Entry { get; }
    public PlantIdentity Identity => Entry.Plant;
    public UnlockPolicy UnlockPolicy { get; }
    public Func<Board, int> Cost { get; }
    public Func<Board, int, int, PlantingReason>? CanPlant { get; set; }
    public Action<Plant>? Initialize { get; set; }
    public Action<Plant>? Tick { get; set; }
    public Action<Projectile>? ProjectileCreated { get; set; }
    public Func<Sprite?>? Portrait { get; set; }

    internal PlantDefinition(ModCatalog mod, string id, Entry entry, Func<Board, int> cost, UnlockPolicy unlock) {
        Mod = mod; Id = id; Entry = entry; Cost = cost; UnlockPolicy = unlock;
    }
    public string Name => LibraryAlmanac.GermanGameLanguage() ? Entry.German.Name : Entry.English.Name;
}

public static class Plants {
    private static readonly List<PlantDefinition> definitions = new();
    private static readonly Dictionary<PlantIdentity, PlantDefinition> identities = new();
    public static IReadOnlyList<PlantDefinition> All => definitions.AsReadOnly();
    public static Func<Board, bool>? ExternalChooser { get; set; }
    public static void RegisterCatalog(ModCatalog mod) {
        if (LibraryAlmanac.Catalogs.Any(m => m.Id == mod.Id)) throw new InvalidOperationException($"Duplicate mod ID: {mod.Id}");
        LibraryAlmanac.Catalogs.Add(mod);
    }
    public static PlantDefinition RegisterPlant(ModCatalog mod, string id, Entry entry, Func<Board, int> cost,
        UnlockPolicy unlock = UnlockPolicy.Always) {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Plant ID cannot be empty.");
        if (!Enum.IsDefined(typeof(SeedType), entry.Plant.NativeType) || entry.Plant.NativeType < 0
            || entry.Plant.NativeType >= SeedType.NumSeedTypes || entry.Plant.NativeType == SeedType.Imitater
            || (entry.Plant.Marker != SeedType.None && (!Enum.IsDefined(typeof(SeedType), entry.Plant.Marker)
                || entry.Plant.Marker < 0 || entry.Plant.Marker >= SeedType.NumSeedTypes)))
            throw new ArgumentException("Use valid native seed types and variant markers; never synthetic enum IDs.");
        if (definitions.Any(p => p.Mod.Id == mod.Id && (p.Id == id || p.Entry.SourceId == entry.SourceId)))
            throw new InvalidOperationException($"Duplicate plant or portrait ID in {mod.Id}: {id}");
        if (entry.Plant.Marker != SeedType.None && identities.ContainsKey(entry.Plant))
            throw new InvalidOperationException($"Plant identity already owned: {entry.Plant}");
        var result = new PlantDefinition(mod, id, entry, cost ?? throw new ArgumentNullException(nameof(cost)), unlock);
        definitions.Add(result);
        if (entry.Plant.Marker != SeedType.None) identities.Add(entry.Plant, result);
        return result;
    }
    public static PlantDefinition? Find(PlantIdentity identity) => identity.Marker == SeedType.None ? null
        : identities.GetValueOrDefault(identity);
    public static PlantDefinition? Find(Plant plant) => Find(new PlantIdentity(plant.mSeedType, plant.mImitaterType));
    public static bool IsUnlocked(PlantDefinition plant, Board board) => DebugCatalog.PlantUnlock(plant)
        ?? (plant.UnlockPolicy == UnlockPolicy.Always || plant.UnlockPolicy == UnlockPolicy.Explicit && UnlockStore.Contains(plant, board));
    public static void Unlock(PlantDefinition plant, Board board) {
        if (plant.UnlockPolicy != UnlockPolicy.Explicit) throw new InvalidOperationException("This plant does not support explicit unlocks.");
        UnlockStore.Add(plant, board);
    }
}
