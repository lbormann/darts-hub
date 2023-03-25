#!/bin/bash


# Äquivalent zu "timeout 2 /nobreak"
sleep 2

# Äquivalent zu "xcopy /s/e/v/y/z .\Updates\ ."
cp -R -p -n -v ./Updates/* .

# Äquivalent zu "rmdir /s /q .\Updates"
rm -rf ./Updates

# Äquivalent zu "start "Autodarts-Desktop" /max autodarts-desktop.exe"
# Anpassen, falls der Pfad oder der Dateiname auf Linux/macOS unterschiedlich ist
./autodarts-desktop