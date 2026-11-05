# Kartenkorrektur 1.0.1

Die Screenshots vom 3. November 2026, 07:21:22 und 07:21:35, zeigen native Samenpäckchen gegenüber freigestellten Mod-Porträts. Ursache war die Verwendung von SPR_Almanac_Slots als Kartenbasis. Diese Grafik stellt leere Rasterplätze dar. SPR_Almanac_Seedpackets_Empty ist eine weiße Maskenfläche und ebenfalls keine Kartenbasis.

Die Bibliothek verwendet jetzt die bereits vorhandene, aus nativen Assets abgeleitete Kartenbasis packet-background.png. Mod-Porträts liegen innerhalb der Papierfläche. Native vollständige Miniaturen erhalten keinen weiteren Rahmen. Die originale OverState-Maske und die native ColorBlock-Steuerung bleiben für Auswahl und Hover erhalten. Separate Namen im Raster entfallen; Namen und Informationen bleiben in der Detailansicht. Padding und Ausrichtung stammen ebenfalls aus dem nativen GridLayoutGroup.

Die Bibliothek besitzt die gemeinsam verwendete Kartenbasis und gibt Sprite und Textur nach dem Schließen ihrer Ansichten beim Entladen frei. Öffentliche API, Pflanzenkennungen und Spielregeln wurden nicht verändert. Die Version steigt von 1.0.0 auf 1.0.1.

Release-Build gegen die installierten Referenzen: 0 Fehler, 0 Warnungen. Die statische Vorschau almanac-cards-1.0.1-preview.png zeigt die Bildkomposition ohne Unity-Laufzeit. Sie ist kein Spiel-Screenshot. Spiel und Testprogramme wurden nicht gestartet. Auswahl, Hover und Controller-Navigation sind durch den Benutzer im Spiel zu prüfen.

Nur PlantLibrary.dll wurde ersetzt. FusionMod.dll, ZombieLibrary.dll und LawnGlove.dll sind per SHA256 unverändert. Vorherige DLL und Installationsnachweis liegen unter:

`/home/kevin/modding/pvzreplanted/plant-library/backups/20261103T063252.075338Z`
