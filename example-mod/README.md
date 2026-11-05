# Plant Library Example

The example mod implements a “Robust Pea Shooter” exclusively via the public Plant Library API. It uses the native pea shooter, has 450 health, costs 125 suns and retains native damage and firing rhythm. Your peas gain a saved marker as well as an additional native snow impact effect and puff sound. The mod automatically receives an almanac tab and a place or an additional page in the regular selection.

The variant pair is `Peashooter / Imitater`. The projectile marker `mCobTargetX = -700001` only applies to your own normal peas. Both identifiers must remain stable after publication. There are no fusions and no custom Harmony patches.

```sh
rtk proxy /home/kevin/modding/pvzreplanted/.tools/dotnet/dotnet build /home/kevin/modding/pvzreplanted/plant-library/example-mod/PlantLibraryExample.csproj -c Release
rtk proxy python3 /home/kevin/modding/pvzreplanted/plant-library/tools/install.py --example
```

The sample mod is not installed by default. To remove, only remove `Mods/PlantLibraryExample.dll` while the game is closed. For saved games with example plants, leave the example mod installed.

The user is responsible for play testing. When expanding, first clarify the saved state and native lifespan, then add required general functions in Plant Library. Specific plant abilities remain in the example mod.
