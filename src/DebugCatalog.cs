using Il2CppReloaded.Gameplay;
using UnityEngine;

namespace PlantLibrary;

// Native delegate/tuple contract keeps DebugMod optional in both directions.
// Snapshot every registered definition, including entries locked in normal play.
internal static class DebugCatalog
{
    internal static IEnumerable<(SeedType NativeType, SeedType Marker, string Name, Func<Sprite?> Portrait,
        Func<Board, int> Cost, Func<Board, int, int, PlantingReason>? CanPlant, string Owner)> Entries() =>
        Plants.All.Select(plant => (plant.Identity.NativeType, plant.Identity.Marker, plant.Name,
            (Func<Sprite?>)(() => Portraits.Get(plant)), plant.Cost, plant.CanPlant, plant.Mod.Id)).ToArray();

    private static readonly Dictionary<(string Owner, string Id), bool> plantUnlocks = new(), almanacUnlocks = new();
    internal static int Revision { get; private set; }
    internal static bool? PlantUnlock(PlantDefinition plant) => plantUnlocks.TryGetValue((plant.Mod.Id, plant.Id), out bool value) ? value : null;
    internal static bool CanView(PlantDefinition plant) => !almanacUnlocks.TryGetValue((plant.Mod.Id, plant.Id), out bool value) || value;
    internal static bool CanView(ModCatalog owner, PlantIdentity identity) => Plants.All.FirstOrDefault(p => p.Mod == owner && p.Identity == identity) is not { } plant || CanView(plant);
    internal static void SetUnlock(string owner, string id, bool unlocked, bool almanac)
    {
        var plant = Plants.All.FirstOrDefault(p => p.Mod.Id == owner && p.Id == id)
            ?? throw new ArgumentException("Unknown registered plant.");
        (almanac ? almanacUnlocks : plantUnlocks)[(owner, id)] = unlocked;
        ++Revision;
    }
    internal static void ResetUnlocks() { plantUnlocks.Clear(); almanacUnlocks.Clear(); ++Revision; }
    internal static IEnumerable<(string Owner, string Id, string Name, string Info, SeedType NativeType, SeedType Marker, bool AlmanacUnlocked, bool? PlantUnlocked)> AlmanacEntries(Board? board) =>
        Plants.All.Select(plant => (plant.Mod.Id, plant.Id, plant.Name,
            LibraryAlmanac.GermanGameLanguage() ? plant.Entry.German.Info : plant.Entry.English.Info,
            plant.Identity.NativeType, plant.Identity.Marker, CanView(plant), board == null ? PlantUnlock(plant) : Plants.IsUnlocked(plant, board))).ToArray();
}
