# Statischer Abschlussnachweis

Plant Library 1.0.0 und Plant Fusion 19.0.0 wurden gemeinsam installiert. Release-Builds gegen die installierten IL2CPP-Referenzen und der Build von PlantLibraryExample endeten jeweils mit 0 Fehlern und 0 Warnungen. Die Beispiel-Mod ist gebaut, aber nicht installiert.

Die generierten Fusion-Registrierungen stimmen mit dem Manifest überein. 296 Katalogeinträge besitzen eindeutige Katalogidentitäten. Projekt-XML und die geänderten Python-Werkzeuge sind syntaktisch gültig. Bestehende Offline-Testadapter wurden auf die neue Abhängigkeit angepasst, aber weder gebaut noch ausgeführt.

Native statische Belege: SeedChooserDataModel.UpdateEntries bei RVA 0x79E150 baut vollständige und freigeschaltete Modelle getrennt auf; die Bibliothek übernimmt daher gemeinsame Referenzen auf die vollständigen Modelle. SeedChooserScreen.PickedPlantType bei RVA 0x6D2700 untersucht die ursprünglichen 49 Pflanzenplätze. Native Auswahlregeln bleiben für diese Einträge erhalten. Die Bibliothek behandelt zusätzliche Karten und die Übernahme ihrer Varianten in die Saatbank separat.

Installierte DLL-Versionen wurden aus PE-Ressourcen gelesen. Installierte Dateien und Build-Ausgaben wurden bytegenau verglichen. SHA256SUMS bestätigt die installierten Dateien. Die Installation sichert vorherige DLLs unter:

`/home/kevin/modding/pvzreplanted/plant-library/backups/20261103T052037.911616Z`

Dort dokumentiert `installation.json` alte und neue Prüfsummen sowie die vorherige Existenz jeder DLL. Die bisherige FusionMod.dll ist gesichert; PlantLibrary.dll war zuvor nicht installiert.

Spiel und Testprogramme wurden nicht gestartet. Zusätzliche Auswahlseiten, Controller-Navigation, Koop, Fortsetzen gespeicherter Partien, Blutsonnenblumen-Freischaltung, Soundhäufigkeit und Menügestaltung sind manuell im Spiel zu prüfen. Kompilierung und Dateiprüfungen belegen diese Laufzeiteigenschaften nicht.
