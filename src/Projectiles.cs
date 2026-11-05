using Il2CppReloaded.Gameplay;
using HarmonyLib;

namespace PlantLibrary;

public sealed record ProjectileImpact(Board Board, float X, float Y, int Row, int RenderOrder, ProjectileType NativeType) {
    public static ProjectileImpact Capture(Projectile shot) => new(shot.mBoard, shot.mPosX, shot.mPosY, shot.mRow, shot.mRenderOrder, shot.mProjectileType);
}
public sealed record ProjectileDefinition(string Id, Func<Projectile, bool> Matches,
    Action<Projectile>? Born = null, string? ImpactEffect = null, string? ImpactSound = null, bool ImpactOnlyWhenOriginalSkipped = false);

/// <summary>Mods keep damage and collision rules. Library owns registered presentation dispatch.</summary>
public static class Projectiles {
    private static readonly Dictionary<string, ProjectileDefinition> definitions = new();
    private static readonly Dictionary<string, Action<ProjectileImpact>> effects = new();
    private static readonly Dictionary<string, Action<Board>> sounds = new();
    private static readonly Dictionary<IntPtr, (IntPtr Board, uint Id)> births = new();
    private static readonly Dictionary<IntPtr, (IntPtr Board, uint Id)> attributed = new();
    internal static void Created(Projectile shot) {
        if (shot == null || shot.mDead || RegisteredFireScopePatch.Current is not {} plant) return;
        var generation = (shot.mBoard.Pointer, (uint)shot.DataID);
        if (attributed.TryGetValue(shot.Pointer, out var old) && old == generation) return;
        attributed[shot.Pointer] = generation; plant.ProjectileCreated?.Invoke(shot); NotifyBorn(shot);
    }
    public static void RegisterProjectile(ProjectileDefinition definition) {
        if (string.IsNullOrWhiteSpace(definition.Id) || definition.Matches == null) throw new ArgumentException("Invalid projectile definition.");
        if (definition.ImpactEffect != null && !effects.ContainsKey(definition.ImpactEffect)) throw new ArgumentException("Register impact effect first.");
        if (definition.ImpactSound != null && !sounds.ContainsKey(definition.ImpactSound)) throw new ArgumentException("Register sound first.");
        definitions.Add(definition.Id, definition);
    }
    public static void RegisterImpactEffect(string id, Action<ProjectileImpact> effect) => effects.Add(id, effect ?? throw new ArgumentNullException(nameof(effect)));
    public static void RegisterSound(string id, Action<Board> sound) => sounds.Add(id, sound ?? throw new ArgumentNullException(nameof(sound)));
    public static void PlaySound(string id, Board board) => sounds[id](board);
    public static void EmitImpactEffect(string id, ProjectileImpact impact) => effects[id](impact);
    public static void NotifyBorn(Projectile shot) {
        if (shot.mDead || shot.mProjectileAge != 0) return;
        var generation = (shot.mBoard.Pointer, (uint)shot.DataID);
        if (births.TryGetValue(shot.Pointer, out var old) && old == generation) return;
        var definition = definitions.Values.FirstOrDefault(p => p.Matches(shot));
        if (definition == null) return;
        births[shot.Pointer] = generation;
        definition.Born?.Invoke(shot);
    }
    internal static ProjectileDefinition? Match(Projectile shot) => shot.mDead ? null : definitions.Values.FirstOrDefault(p => p.Matches(shot));
    internal static void Impact(ProjectileImpact impact, ProjectileDefinition? definition, bool originalRan) {
        if (definition?.ImpactOnlyWhenOriginalSkipped == true && originalRan) return;
        if (definition == null || impact.Board == null) return;
        if (definition.ImpactEffect != null) EmitImpactEffect(definition.ImpactEffect, impact);
        if (definition.ImpactSound != null) PlaySound(definition.ImpactSound, impact.Board);
    }
    internal static void Forget(Board board) {
        foreach (var key in births.Where(p => p.Value.Board == board.Pointer).Select(p => p.Key).ToArray()) births.Remove(key);
        foreach (var key in attributed.Where(p => p.Value.Board == board.Pointer).Select(p => p.Key).ToArray()) attributed.Remove(key);
    }
}
[HarmonyPatch(typeof(Projectile), nameof(Projectile.DoImpact))]
internal static class RegisteredImpactPatch {
    [HarmonyPriority(Priority.First)]
    private static void Prefix(Projectile __instance, out (ProjectileDefinition? Definition, ProjectileImpact Impact) __state) =>
        __state = (Projectiles.Match(__instance), ProjectileImpact.Capture(__instance));
    [HarmonyPriority(Priority.Last)]
    private static void Postfix((ProjectileDefinition? Definition, ProjectileImpact Impact) __state, bool __runOriginal) => Projectiles.Impact(__state.Impact, __state.Definition, __runOriginal);
}
[HarmonyPatch(typeof(Projectile), nameof(Projectile.Update))]
internal static class RegisteredProjectileTickPatch {
    private static void Prefix(Projectile __instance) => Projectiles.NotifyBorn(__instance);
}
[HarmonyPatch(typeof(Plant), nameof(Plant.PlantInitialize))]
internal static class RegisteredPlantInitializePatch {
    private static void Postfix(Plant __instance) => Plants.Find(__instance)?.Initialize?.Invoke(__instance);
}
[HarmonyPatch(typeof(Plant), nameof(Plant.UpdateAbilities))]
internal static class RegisteredPlantTickPatch {
    private static void Postfix(Plant __instance) => Plants.Find(__instance)?.Tick?.Invoke(__instance);
}
[HarmonyPatch(typeof(Board), nameof(Board.DisposeBoard))]
internal static class LibraryBoardCleanupPatch {
    private static void Prefix(Board __instance) { Projectiles.Forget(__instance); SeedSelection.Forget(__instance); }
}

// Attribute native shots to the registered plant without changing native shooting or damage.
[HarmonyPatch(typeof(Plant), nameof(Plant.Fire))]
internal static class RegisteredFireScopePatch {
    [ThreadStatic] internal static PlantDefinition? Current;
    private static void Prefix(Plant __instance, out PlantDefinition? __state) { __state = Current; Current = Plants.Find(__instance); }
    private static void Finalizer(PlantDefinition? __state) => Current = __state;
}
[HarmonyPatch(typeof(Board), nameof(Board.AddProjectile), new[] { typeof(float), typeof(float), typeof(int), typeof(int), typeof(ProjectileType) })]
internal static class RegisteredShotCreatedPatch {
    private static void Postfix(Projectile __result) => Projectiles.Created(__result);
}
[HarmonyPatch(typeof(Board), nameof(Board.AddProjectile), new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(ProjectileType) })]
internal static class RegisteredIntegerShotCreatedPatch {
    private static void Postfix(Projectile __result) => Projectiles.Created(__result);
}
