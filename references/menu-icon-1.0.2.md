# Menu icon 1.0.2

Reference: Screenshot_20261103_073551.png. The previous grass area was behind all the descriptive text, and the icon showed a seed packet.

The export now uses the native sprite SPR_Preview_PeaShooter (Path ID 3333466314890536487) with original alpha channel. The Peashooter is set to a square section of the native Area1_Lawn. The lawn is completely opaque, without the previous pale brightening. The resulting menu-icon.png is registered via BloomEngine.AddIcon. The native menu takes over the position and frame. UI objects are no longer created behind the text; the periodic UI scan is no longer necessary.

The export sources and checksums are in assets/sources.json. The graphic has 512 × 512 pixels and full opacity. peashooter.png still has transparent exterior areas and does not contain a seed packet.

Release build: 0 errors, 0 warnings. Installed DLL version and file checksums confirmed. Only PlantLibrary.dll was replaced. Game and test programs did not start. The change will appear the next time you load the mod; the runtime check remains with the user.

Previous DLL and installation proof:

`/home/kevin/modding/pvzreplanted/plant-library/backups/20261103T064233.205648Z`
