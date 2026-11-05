# Plant Library Example

Die Beispiel-Mod implementiert einen „Robusten Erbsenschützen“ ausschließlich über die öffentliche Plant-Library-API. Sie nutzt den nativen Erbsenschützen, hat 450 Lebenspunkte, kostet 125 Sonnen und behält nativen Schaden sowie Schussrhythmus. Ihre Erbsen erhalten einen gespeicherten Marker sowie einen zusätzlichen nativen Schnee-Aufpralleffekt und Puff-Sound. Die Mod erhält automatisch einen Almanach-Tab und einen Platz beziehungsweise eine zusätzliche Seite in der regulären Auswahl.

Das Variantenpaar ist `Peashooter / Imitater`. Der Projektilmarker `mCobTargetX = -700001` gilt nur für die eigenen normalen Erbsen. Beide Kennungen müssen nach Veröffentlichung stabil bleiben. Es gibt keine Fusionen und keine eigenen Harmony-Patches.

```sh
rtk proxy /home/kevin/modding/pvzreplanted/.tools/dotnet/dotnet build /home/kevin/modding/pvzreplanted/plant-library/example-mod/PlantLibraryExample.csproj -c Release
rtk proxy python3 /home/kevin/modding/pvzreplanted/plant-library/tools/install.py --example
```

Die Beispiel-Mod wird standardmäßig nicht installiert. Zum Entfernen ausschließlich `Mods/PlantLibraryExample.dll` entfernen, während das Spiel geschlossen ist. Für gespeicherte Partien mit Beispielpflanzen die Beispiel-Mod weiter installiert lassen.

Spieltests übernimmt der Benutzer. Beim Erweitern zuerst den gespeicherten Zustand und die native Lebensdauer klären, dann benötigte allgemeine Funktionen in Plant Library ergänzen. Konkrete Pflanzenfähigkeiten bleiben in der Beispiel-Mod.
