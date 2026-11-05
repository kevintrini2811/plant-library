# Menü-Icon 1.0.2

Bezug: Bildschirmfoto_20261103_073551.png. Die bisherige Rasenfläche lag hinter dem gesamten Beschreibungstext, und das Icon zeigte ein Samenpäckchen.

Der Export verwendet nun den nativen Sprite SPR_Preview_PeaShooter (Path ID 3333466314890536487) mit originalem Alphakanal. Der Peashooter wird auf einen quadratischen Ausschnitt des nativen Area1_Lawn gesetzt. Der Rasen ist vollständig deckend, ohne die bisherige blasse Aufhellung. Das resultierende menu-icon.png wird über BloomEngine.AddIcon registriert. Das native Menü übernimmt Position und Rahmen. Es werden keine UI-Objekte mehr hinter dem Text erzeugt; der periodische UI-Scan entfällt.

Die Exportquellen und Prüfsummen stehen in assets/sources.json. Die Grafik besitzt 512 × 512 Pixel und vollständige Deckkraft. peashooter.png besitzt weiterhin transparente Außenbereiche und enthält kein Samenpäckchen.

Release-Build: 0 Fehler, 0 Warnungen. Installierte DLL-Version und Dateiprüfsummen bestätigt. Nur PlantLibrary.dll wurde ersetzt. Spiel und Testprogramme wurden nicht gestartet. Die Änderung erscheint beim nächsten Laden der Mod; die Laufzeitprüfung bleibt beim Benutzer.

Vorherige DLL und Installationsnachweis:

`/home/kevin/modding/pvzreplanted/plant-library/backups/20261103T064233.205648Z`
