#!/bin/bash

# Wechsel in das aktuelle Verzeichnis
cd "$(dirname "$0")"

# Warte 2 Sekunden
sleep 2

# Kopiere alle Dateien und Verzeichnisse aus dem Ordner Updates in das aktuelle Verzeichnis
cp -r Updates/* .

# Lösche den Ordner Updates
rm -rf Updates

# Starte die Anwendung "autodarts-desktop"
./autodarts-desktop