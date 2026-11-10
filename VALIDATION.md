# Static proof of completion

Plant Library 1.0.0 and Plant Fusion 19.0.0 were installed together. Release builds against the installed IL2CPP references and the PlantLibraryExample build ended with 0 errors and 0 warnings, respectively. The example mod is built but not installed.

The Fusion registrations generated match the manifest. 296 catalog entries have unique catalog identities. Project XML and the modified Python tools are syntactically valid. Existing offline test adapters were adapted to the new dependency, but were neither built nor executed.

Native static evidence: SeedChooserDataModel.UpdateEntries at RVA 0x79E150 builds complete and unlocked models separately; the library therefore adopts common references to the complete models. SeedChooserScreen.PickedPlantType at RVA 0x6D2700 examines the original 49 plant slots. Native selection rules are retained for these entries. The library handles additional cards and the addition of their variants to the seed bank separately.

Installed DLL versions were read from PE resources. Installed files and build output were compared down to the last byte. SHA256SUMS confirms the installed files. The installation saves previous DLLs at:

`/home/kevin/modding/pvzreplanted/plant-library/backups/20261103T052037.911616Z`

There, `installation.json` documents old and new checksums as well as the previous existence of each DLL. The previous FusionMod.dll is secured; PlantLibrary.dll was not previously installed.

Game and test programs did not start. Additional selection pages, controller navigation, co-op, resuming saved games, Blood Sunflower unlock, sound frequency and menu design must be checked manually in the game. Compilation and file checks do not demonstrate these runtime properties.
