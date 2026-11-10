using MelonLoader;
[assembly: MelonInfo(typeof(PlantLibrary.LibraryMod), "Plant Library", "1.0.4", "Kevin / Codex")]
[assembly: MelonGame("PopCap Games", "PvZ Replanted")]
[assembly: MelonOptionalDependencies("DebugMod")]
[assembly: MelonAdditionalDependencies("BloomEngine")]
[assembly: MelonPriority(-100)]
namespace PlantLibrary;
public sealed class LibraryMod : MelonMod {
    internal static LibraryMod Instance = null!;
    public override void OnInitializeMelon() { Instance = this; MenuPresentation.Register(this); }
    public override void OnLateUpdate() { LibraryAlmanac.Update(); SeedSelection.Update(); }
    public override void OnDeinitializeMelon() { LibraryAlmanac.Dispose(); SeedSelection.Clear(); Portraits.Dispose(); MenuPresentation.Dispose(); }
}
