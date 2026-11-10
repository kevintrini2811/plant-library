using Il2CppReloaded.Gameplay;
using Il2CppReloaded.Services;
using MelonLoader;
using PlantLibrary;

[assembly: MelonInfo(typeof(PlantLibraryExample.ExampleMod), "Plant Library Example", "1.0.0", "Kevin / Codex")]
[assembly: MelonGame("PopCap Games", "PvZ Replanted")]
[assembly: MelonAdditionalDependencies("PlantLibrary")]
namespace PlantLibraryExample;

/// <summary>A native peashooter variant with more health and registered impact presentation.</summary>
public sealed class ExampleMod : MelonMod {
    private const float ProjectileMarker = -700001;
    public override void OnInitializeMelon() {
        var mod = new ModCatalog("plant-library-example", "Example Plants", name =>
            typeof(ExampleMod).Assembly.GetManifestResourceStream(name) ?? throw new InvalidDataException(name));
        var plant = Plants.RegisterPlant(mod, "sturdy-peashooter", new Entry(1,
            new PlantIdentity(SeedType.Peashooter, SeedType.Imitater),
            new EntryText("Sturdy Peashooter", "450 health. Native pea damage and firing rhythm. Cost: 125 sun."),
            new EntryText("Robuster Erbsenschütze", "450 Lebenspunkte. Nativer Erbsenschaden und Schussrhythmus. Kosten: 125 Sonnen."),
            "PlantLibraryExample.Assets.peashooter.png"), _ => 125, UnlockPolicy.Always);
        plant.Initialize = value => value.mPlantHealth = value.mPlantMaxHealth = 450;
        plant.ProjectileCreated = shot => { if (shot.mProjectileType == ProjectileType.Pea) shot.mCobTargetX = ProjectileMarker; };
        Projectiles.RegisterSound("example:impact", board => board.mApp.SoundSystem.PlayFoley(FoleyType.Puff));
        Projectiles.RegisterImpactEffect("example:spark", impact =>
            impact.Board.mApp.AddTodParticle(impact.X, impact.Y, impact.RenderOrder, ParticleEffect.SnowpeaSplat));
        Projectiles.RegisterProjectile(new ProjectileDefinition("example:pea",
            shot => shot.mProjectileType == ProjectileType.Pea && shot.mCobTargetX == ProjectileMarker,
            ImpactEffect: "example:spark", ImpactSound: "example:impact"));
        Plants.RegisterCatalog(mod);
    }
}
