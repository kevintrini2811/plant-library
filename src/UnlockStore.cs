using System.Text.Json;
using Il2CppReloaded.Gameplay;
using MelonLoader.Utils;

namespace PlantLibrary;

// Sidecar progress never changes native achievements, seed unlocks or save enums.
internal static class UnlockStore {
    private static readonly string FilePath = Path.Combine(MelonEnvironment.UserDataDirectory, "PlantLibrary", "unlocks.json");
    private static HashSet<string>? unlocked;
    private static bool loadFailed;
    private static string? Key(PlantDefinition plant, Board board) {
        string? profile = board.mApp?.UserService?.ActiveUserProfile?.mGuid;
        return string.IsNullOrEmpty(profile) ? null : profile + "/" + plant.Mod.Id + "/" + plant.Id;
    }
    private static void Load() {
        if (unlocked != null) return;
        try { unlocked = File.Exists(FilePath) ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(FilePath))
                ?? throw new InvalidDataException("Invalid PlantLibrary unlock file.") : new(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException) {
            unlocked = new(); loadFailed = true;
            LibraryMod.Instance.LoggerInstance.Error($"Cannot read unlocks; existing file retained: {error.Message}");
        }
    }
    internal static bool Contains(PlantDefinition plant, Board board) { Load(); return Key(plant, board) is {} key && unlocked!.Contains(key); }
    internal static void Add(PlantDefinition plant, Board board) {
        Load(); if (loadFailed || Key(plant, board) is not {} key || unlocked!.Contains(key)) return;
        var updated = new HashSet<string>(unlocked) { key };
        string temporary = FilePath + ".tmp";
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(updated)); File.Move(temporary, FilePath, true); unlocked = updated;
        } catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
            // A progress-storage failure must not interrupt a committed gameplay transaction.
            LibraryMod.Instance.LoggerInstance.Error($"Cannot save plant unlock: {error.Message}");
        } finally {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
