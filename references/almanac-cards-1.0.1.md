# Map Correction 1.0.1

The screenshots from November 3, 2026, 07:21:22 and 07:21:35, show native seed packets versus cropped mod portraits. The cause was the use of SPR_Almanac_Slots as the map base. This graphic represents empty grid spaces. SPR_Almanac_Seedpackets_Empty is a white mask area and also not a map base.

The library now uses the existing map base packet-background.png derived from native assets. Mod portraits lie within the paper area. Native full miniatures do not receive an additional frame. The original OverState mask and native ColorBlock control remain for selection and hover. Separate names in the grid are no longer required; Names and information remain in the detailed view. Padding and alignment also come from the native GridLayoutGroup.

The library owns the shared map base and releases the sprite and texture upon unloading after closing their views. Public API, plant identifiers and game rules have not been changed. The version increases from 1.0.0 to 1.0.1.

Release build against the installed references: 0 errors, 0 warnings. The static preview almanac-cards-1.0.1-preview.png shows the image composition without Unity runtime. It is not a game screenshot. Game and test programs did not start. Selection, hover and controller navigation are to be checked by the user in the game.

Only PlantLibrary.dll was replaced. FusionMod.dll, ZombieLibrary.dll and LawnGlove.dll are unchanged via SHA256. Previous DLL and installation proof can be found at:

`/home/kevin/modding/pvzreplanted/plant-library/backups/20261103T063252.075338Z`
