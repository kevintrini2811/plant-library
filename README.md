# Plant Library 1.0.2

Gemeinsame MelonLoader-Bibliothek für PvZ Replanted. Gemeinsame native UI- und Lokalisierungshelfer werden als Quellcode aus `../shared/` eingebunden. Die Bibliothek benötigt das vorhandene BloomEngine für ihren Eintrag im Mod-Menü. Native Datenmodelle, Saatbank, Schriftarten, Buttons und Navigation bleiben in Verwendung. Der Menüeintrag zeigt den freigestellten nativen Peashooter auf kräftigem Tagesrasen ausschließlich innerhalb des Icons. Position und Rahmen stammen vom nativen Mod-Menü; hinter dem Beschreibungstext wird kein eigenes UI angelegt.

## Korrektur 1.0.2

Das Menü-Icon verwendet `SPR_Preview_PeaShooter` mit originalem Alphakanal statt eines Samenpäckchens. Der native Tagesrasen wird ohne blasse Aufhellung und mit voller Deckkraft in das quadratische Icon eingebunden. Die bisherige zusätzliche Rasenfläche hinter dem Text und der dafür erforderliche UI-Scan entfallen. `tools/export_menu_assets.py` erzeugt die eingebettete Grafik aus nativen Assets; `assets/sources.json` dokumentiert Sprite-IDs und Prüfsummen.

## Korrektur 1.0.1

Die Pflanzenkarten im Mod-Almanach verwenden jetzt die vorhandene native Kartenbasis mit Rahmen, Papier und „Bloom & Doom“-Kopf. Freigestellte Mod-Porträts werden innerhalb der Kartenfläche dargestellt. Native vollständige Miniaturen bleiben unverändert und erhalten keinen zweiten Rahmen. Die native Auswahlmaske liegt über der vollständigen Karte; Namen stehen wie bei nativen Pflanzen in der Detailansicht. Das Raster übernimmt auch Padding und Ausrichtung des nativen Layouts. Kartenbasis und Quellbezug stehen unter `assets/packet-background.png` und `assets/packet-background-source.json`.

Spiel und Testprogramme werden nicht gestartet. Kompilierung und Dateiansichten ersetzen die manuelle Prüfung der Karten im Spiel nicht.

## Öffentliche API

- `Plants.RegisterPlant(mod, id, entry, cost, unlockPolicy)` registriert eine Pflanze. `PlantIdentity` besteht aus einem vorhandenen nativen `SeedType` und einem gespeicherten Variantenmarker. Doppelte Variantenpaare und IDs werden abgelehnt. IDs nach Veröffentlichung nicht ändern.
- `Plants.RegisterCatalog(mod)` veröffentlicht den Mod-Katalog. Die Bibliothek erzeugt automatisch einen eigenen Almanach-Tab mit übersetzten Texten, Porträts, Seitennavigation und Controller-Navigation. Jede Mod liefert ihre eigenen Ressourcen über `ModCatalog.Resource`.
- `PlantDefinition.Initialize`, `Tick` und `ProjectileCreated` ergänzen native Initialisierung, Fähigkeitsticks und aus `Plant.Fire` erzeugte Projektile. `CanPlant` ergänzt die Terrainprüfung. Die Bibliothek verändert nicht automatisch Schaden oder Verhalten von Projektilen. Eigene Controller und Grafiken bleiben Aufgabe der jeweiligen Mod.
- `Projectiles.RegisterProjectile` registriert eine eindeutige Projektilkennung, eine Erkennungsfunktion sowie optionale Geburt-, Aufpralleffekt- und Sound-Zuordnungen. Kennungen müssen nach Speichern/Laden aus serialisierten Feldern erkennbar sein. Skriptgesteuerte Erzeuger rufen nach Setzen ihrer Kennung `Projectiles.NotifyBorn` auf. Native Schüsse registrierter Pflanzen werden automatisch zugeordnet.
- `Projectiles.RegisterImpactEffect` registriert einen visuellen Aufpralleffekt. `ProjectileImpact` enthält Position, Board und Zeichenreihenfolge vor der Kollision; nach Schadens- oder Todescallbacks wird kein möglicherweise wiederverwendetes Projektil gelesen. `EmitImpactEffect` erlaubt denselben Effekt aus einer eigenen, bereits vorhandenen Kollisionsroutine.
- `Projectiles.RegisterSound` und `PlaySound` registrieren und spielen Sounds. Native Foley-Sounds können direkt in den Callback eingebunden werden. Andere Soundquellen sind ebenfalls möglich; deren Laden und Freigabe übernimmt die Mod.

Effekte und Sounds müssen vor ihrer Projektildefinition registriert werden. Pflanzen müssen vor `RegisterCatalog` registriert werden. Verwende für Ressourcenkennungen den Mod-Präfix, beispielsweise `example:pea`. Die erste passende Projektildefinition besitzt den Effekt; Erkennungsfunktionen dürfen sich deshalb nicht überlappen. `ImpactOnlyWhenOriginalSkipped` verhindert doppelte Sounds, wenn eine vorhandene Kollisionsroutine den nativen Aufprall ersetzt.

`Initialize` gilt für neue Pflanzen, nicht für das Laden gespeicherter Pflanzen. Laufender Zustand gehört in kompatible, belegte Speicherfelder. `Tick` folgt den nativen Simulationsticks. Die Bibliothek bietet keine neue Speicherstruktur innerhalb nativer Spielstände und keine synthetischen Enum-Werte.

## Freischaltungen und Pflanzenauswahl

`UnlockPolicy.Always` stellt eine Variante regulär bereit. `Never` hält sie außerhalb der normalen Auswahl. `Explicit` erlaubt `Plants.Unlock(definition, board)` für das aktive Spielprofil. `IsUnlocked` fragt denselben Zustand ab. Normale Fortschrittsbelohnungen können Varianten nicht freischalten. Bibliotheksfortschritt liegt separat unter `UserData/PlantLibrary/unlocks.json`; native Profile und Spielstände bleiben unberührt. Beschädigte Fortschrittsdateien werden nicht überschrieben.

Die normale Auswahl füllt zuerst freie Plätze ihrer ersten Seite. Erst bei Überlauf erscheinen native Seitenpfeile. Die Imitater-Auswahl bleibt auf ihre nativen Einträge begrenzt. Auswahl und Saatbank unterscheiden Varianten anhand der konkreten Kartenobjekte und der gespeicherten Marker. Die Bibliothek überträgt Varianten nach dem nativen Bestätigen erneut in die Saatbank, weil der native Abschluss nur die ursprünglichen 49 Karten besucht. Kosten, Sonnenabzug und Terrainprüfung bleiben verbindlich.

Die verfügbaren Varianten werden beim Aufbau einer Auswahl aufgenommen. Nach einer neuen Freischaltung die Auswahl erneut öffnen. `Plants.ExternalChooser` erlaubt einer Mod mit eigener vollständiger Auswahl, für ihre Boards die reguläre Bibliotheksauswahl auszusetzen. Plant Fusion nutzt dies ausschließlich für ihren Debug-Level.

Plant Fusion registriert ihre Katalogpflanzen über diese API. Rezeptauflösung, Zutatenverbrauch, Fusionen, Schaden, besondere Terrainregeln und Fusion-Animationen bleiben in Plant Fusion. Vanilla-Pflanzen, die zugleich Fusionsergebnisse sind, behalten ihre normalen nativen Fortschrittsregeln; die Sperre betrifft zusätzliche Varianten. Die Blutsonnenblume wird ausschließlich am erfolgreichen Fusion-Abschluss freigeschaltet. Laden, Infektion und direktes Debug-Spawnen lösen keine Freischaltung aus.

## Bauen und installieren

```sh
rtk proxy /home/kevin/modding/pvzreplanted/.tools/dotnet/dotnet build /home/kevin/modding/pvzreplanted/plant-library/PlantLibrary.csproj -c Release
rtk proxy python3 /home/kevin/modding/pvzreplanted/plant-library/tools/install.py
```

Der Installer baut Fusion, Plant Library und Zombie Library gemeinsam, sichert vorhandene DLLs, prüft konkurrierende Änderungen und ersetzt sie atomar. Bei Fehlern während der DLL-Installation stellt er die bisherigen Dateien wieder her. Er startet weder Spiel noch Testprogramme. Prüfsummen und Installationsnachweis liegen unter `SHA256SUMS` und `backups/<Zeitstempel>/installation.json`.

Die [Beispiel-Mod](example-mod/README.md) wird mit `--example` zusätzlich installiert. Sie ist standardmäßig nur Quellcode und gebautes Beispiel, damit die Demonstrationspflanze nicht ungefragt in regulären Partien erscheint.

Für einen Rückbau zuerst das Spiel schließen. Die im Installationsnachweis aufgeführten alten DLLs aus dem Backup wieder nach `Mods/` kopieren. Dateien, deren vorheriger Hash `null` ist, waren vorher nicht installiert und können entfernt werden. Fusion, Plant Library und Zombie Library immer gemeinsam zurückbauen. Eine Bibliothek nicht entfernen, während eine abhängige Mod installiert bleibt.

## Belege und offene Laufzeitprüfung

Builds verwenden die tatsächlich installierten IL2CPP-Referenzen. Der Tagesrasen stammt aus `backgroundatlases_assets_all_19e402bc532d2cf4f6d062dcf0b108ec.bundle`, Sprite `Area1_Lawn`, Path ID `-5079667051938085663`. Der Menüausschnitt zeigt ausschließlich Rasen und wird auf 1450 × 600 Pixel begrenzt. `tools/export_menu_assets.py` und `assets/sources.json` dokumentieren Export und Prüfsummen. Der freigestellte Peashooter stammt aus dem nativen Sprite `SPR_Preview_PeaShooter`, Path ID `3333466314890536487`, im Bundle `defaultlocalgroup_assets_assets/art/characters/previews_f999564bc5382eb85495ec50c887a03d.bundle`. Grafiken werden nicht generiert oder aus dem Internet geladen.

Spiel und Testprogramme werden auf Benutzerwunsch nicht ausgeführt. Insbesondere zusätzliche Auswahlseiten, Controller-Navigation, Koop, Fortsetzen gespeicherter Partien, Soundhäufigkeit und Menügestaltung benötigen die manuelle Laufzeitprüfung. Dies ist keine Behauptung bereits bestandener Spieltests.

## Debug-API

Die optionale Debug Mod 1.1.0 kann registrierte Almanacheinträge abfragen und deren Sichtbarkeit für die laufende Sitzung überschreiben. Plant Library unterstützt zusätzlich sitzungsbezogene Overrides der Pflanzenauswahl. Ohne Debug Mod gelten unverändert die normalen Bibliotheksregeln. Anleitung: `../debug-mod/src/documentation.md`.
