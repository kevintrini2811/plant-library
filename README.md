# Plant Library 1.0.2

## Dependencies

- Game/runtime: PvZ Replanted (Windows/IL2CPP), MelonLoader 0.7.3, and .NET 6.
- Required mods: `BloomEngine.dll`.
- Optional integrations: `DebugMod`.
- Building: .NET SDK with `net6.0` support and the game/MelonLoader reference assemblies declared in `PlantLibrary.csproj`.

Shared MelonLoader library for PvZ Replanted. Common native UI and localization helpers are included as source code from `../shared/`. The library requires the existing BloomEngine for its entry in the mod menu. Native data models, seed bank, fonts, buttons and navigation remain in use. The menu entry shows the isolated native peashooter on strong day lawn exclusively within the icon. Position and frame are from the native mod menu; No separate UI is created behind the description text.

## Fix 1.0.2

The menu icon uses `SPR_Preview_PeaShooter` with original alpha channel instead of a seed packet. The native day lawn is integrated into the square icon without any pale brightening and with full opacity. The previous additional grass area behind the text and the UI scan required for it are no longer necessary. `tools/export_menu_assets.py` creates the embedded graphics from native assets; `assets/sources.json` documents sprite IDs and checksums.

## Fix 1.0.1

The plant cards in the Mod Almanac now use the existing native card base with frame, paper and Bloom & Doom header. Isolated mod portraits are displayed within the card area. Native full thumbnails remain unchanged and do not receive a second frame. The native selection mask overlays the full map; As with native plants, names are in the detailed view. The grid also handles padding and alignment from the native layout. Map base and source reference are under `assets/packet-background.png` and `assets/packet-background-source.json`.

Game and test programs will not start. Compilation and file views do not replace manual review of in-game maps.

## Public API

- `Plants.RegisterPlant(mod, id, entry, cost, unlockPolicy)` registers a plant. `PlantIdentity` consists of an existing native `SeedType` and a saved variant marker. Duplicate variant pairs and IDs are rejected. Do not change IDs after publishing.
- `Plants.RegisterCatalog(mod)` publishes the mod catalog. The library automatically creates its own almanac tab with translated text, portraits, page navigation and controller navigation. Each mod provides its own resources via `ModCatalog.Resource`.
- `PlantDefinition.Initialize`, `Tick` and `ProjectileCreated` add native initialization, ability ticks and projectiles generated from `Plant.Fire`. `CanPlant` complements the terrain test. The library does not automatically change projectile damage or behavior. Own controllers and graphics remain the responsibility of the respective mod.
- `Projectiles.RegisterProjectile` registers a unique projectile identifier, a detection function and optional birth, impact effect and sound mappings. Identifiers must be recognizable from serialized fields after save/load. Script-controlled producers call `Projectiles.NotifyBorn` after setting their identifier. Native shots of registered plants are assigned automatically.
- `Projectiles.RegisterImpactEffect` registers a visual impact effect. `ProjectileImpact` contains position, board and character order before collision; no potentially reused projectile is read after damage or death callbacks. `EmitImpactEffect` allows the same effect from its own existing collision routine.
- `Projectiles.RegisterSound` and `PlaySound` register and play sounds. Native Foley sounds can be integrated directly into the callback. Other sound sources are also possible; The mod takes care of loading and releasing them.

Effects and sounds must be registered before their projectile definition. Plants must be registered before `RegisterCatalog`. For resource identifiers, use the mod prefix, for example `example:pea`. The first matching projectile definition has the effect; Recognition functions must therefore not overlap. `ImpactOnlyWhenOriginalSkipped` prevents duplicate sounds when an existing collision routine replaces the native collision.

`Initialize` applies to new plants, not to loading saved plants. Running state belongs in compatible, occupied memory fields. `Tick` follows the native simulation sticks. The library does not provide a new storage structure within native saves and does not provide synthetic enum values.

## Unlocks and plant selection

`UnlockPolicy.Always` provides a variant regularly. `Never` keeps them outside the normal selection. `Explicit` allows `Plants.Unlock(definition, board)` for the active game profile. `IsUnlocked` queries the same status. Normal progression rewards cannot unlock variants. Library progress is separately under `UserData/PlantLibrary/unlocks.json`; Native profiles and saves remain unaffected. Damaged progress files will not be overwritten.

The normal selection first fills free spaces on its first page. Native page arrows only appear when there is an overflow. The impersonator selection remains limited to its native entries. Selection and seed bank distinguish variants based on the specific map objects and the saved markers. The library re-transmits variants into the seed bank after native confirmation because the native completion only visits the original 49 cards. Costs, sun deduction and terrain inspection remain binding.

The available variants are included when building a selection. After a new activation, open the selection again. `Plants.ExternalChooser` allows a mod with its own full selection to suspend the regular library selection for their boards. Plant Fusion uses this exclusively for their debug level.

Plant Fusion registers their catalog plants via this API. Recipe resolution, ingredient consumption, fusions, damage, special terrain rules, and fusion animations remain in Plant Fusion. Vanilla plants that are also fusion results retain their normal native progression rules; the ban affects additional variants. The Blood Sunflower is only unlocked when the Fusion is successfully completed. Loading, infection and direct debug spawning do not trigger an unlock.

## Building and installing

```sh
rtk proxy /home/kevin/modding/pvzreplanted/.tools/dotnet/dotnet build /home/kevin/modding/pvzreplanted/plant-library/PlantLibrary.csproj -c Release
rtk proxy python3 /home/kevin/modding/pvzreplanted/plant-library/tools/install.py
```

The installer builds Fusion, Plant Library and Zombie Library together, backs up existing DLLs, checks competing changes and replaces them atomically. If there are errors during the DLL installation, it restores the previous files. It doesn't start any games or test programs. Checksums and proof of installation are below `SHA256SUMS` and `backups/<Zeitstempel>/installation.json`.

The [example Mod](example-mod/README.md) is also installed with `--example`. By default it is only source code and a built example, so that the demonstration plant does not appear in regular games without being asked.

To restore, first close the game. Copy the old DLLs listed in the installation certificate from the backup back to `Mods/`. Files whose previous hash is `null` were not previously installed and can be removed. Always dismantle Fusion, Plant Library and Zombie Library together. Do not remove a library while leaving a dependent mod installed.

## Documents and open term check

Builds use the actual installed IL2CPP references. The day lawn comes from `backgroundatlases_assets_all_19e402bc532d2cf4f6d062dcf0b108ec.bundle`, Sprite `Area1_Lawn`, Path ID `-5079667051938085663`. The menu section only shows lawns and is limited to 1450 × 600 pixels. `tools/export_menu_assets.py` and `assets/sources.json` document export and checksums. The released peashooter comes from the native sprite `SPR_Preview_PeaShooter`, path ID `3333466314890536487`, in the bundle `defaultlocalgroup_assets_assets/art/characters/previews_f999564bc5382eb85495ec50c887a03d.bundle`. Graphics are not generated or loaded from the Internet.

Game and test programs are not executed at the user's request. In particular, additional selection pages, controller navigation, co-op, resuming saved games, sound frequency and menu design require manual runtime checking. This is not a claim to have already passed playtests.

## Debug-API

The optional Debug Mod 1.1.0 can query registered almanac entries and override their visibility for the current session. Plant Library additionally supports session-related plant selection overrides. Without a debug mod, the normal library rules still apply. Instructions: `../debug-mod/src/documentation.md`.
