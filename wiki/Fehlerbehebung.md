# Fehlerbehebung

## Kein Geraet in der Liste

Pruefen:

1. Ist das Smartphone bereits mit Windows gekoppelt?
2. Ist Bluetooth am PC aktiv?
3. Unterstuetzt der Adapter A2DP Sink?
4. In der App auf **Refresh Devices** klicken.

## Verbindung fehlgeschlagen

Moegliche Ursachen:

- Geraet bereits mit anderem Audio-Sink verbunden
- Bluetooth Stack haengt
- Distanz/Funkstoerungen

Loesung:

1. Disconnect erzwingen
2. Bluetooth auf beiden Geraeten kurz aus/an
3. Neu verbinden

## Kein Ton trotz Verbindung

Pruefen:

1. Standard-Ausgabegeraet in Windows Sound-Einstellungen
2. Lautstaerke auf Smartphone und PC
3. Exklusive Audiomodi anderer Software

## Mediensteuerung reagiert nicht

- Stelle sicher, dass auf dem Smartphone wirklich eine aktive Wiedergabe laeuft.
- Manche Apps/Geraete unterstuetzen AVRCP nur eingeschraenkt.

## App startet nicht

- Mindestversion Windows 10 2004 erforderlich.
- Falls aus Source gebaut: .NET 8 Runtime/SDK installieren.
