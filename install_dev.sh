#!/bin/bash

add_to_autostart() {
    echo "Trying to disable autodarts.service."
    systemctl disable autodarts.service

    echo "Trying to add Autodarts-desktop to autostart."

    case "$PLATFORM" in
        "linux")
            mkdir -p "${HOME}/.config/autostart"
            echo "[Desktop Entry]
            Type=Application
            Name=Autodarts-desktop
            Exec="${HOME}/autodarts-desktop/autodarts-desktop"
            Terminal=false
            NoDisplay=false
            X-GNOME-Autostart-enabled=true
            X-GNOME-Autostart-Delay=8" > "${HOME}/.config/autostart/autodarts-desktop.desktop"
            ;;
        "macOS")
            osascript -e "tell application \"System Events\" to make new login item at end with properties {path:\"${HOME}/autodarts-desktop/autodarts-desktop\", hidden:false}" > /dev/null
            ;;
        *)
            echo "Platform is not 'linux' or 'macOS', and hence autostart configuration is not supported by this script."
            ;;
    esac
}

remove_from_autostart() {
    echo "Trying to enable autodarts.service."
    systemctl enable autodarts.service

    echo "Trying to remove Autodarts-desktop from autostart."
    case "$PLATFORM" in
        "linux")
            if [[ -f "${HOME}/.config/autostart/autodarts-desktop.desktop" ]]; then
                rm "${HOME}/.config/autostart/autodarts-desktop.desktop"
            else
                echo "Autodarts-desktop is not in autostart. Nothing to remove."
            fi
            ;;
        "macOS")
            osascript -e "tell application \"System Events\" to delete login item \"${HOME}/autodarts-desktop/autodarts-desktop\"" > /dev/null
            ;;
        *)
            echo "Platform is not 'linux' or 'macOS', and hence autostart configuration is not supported by this script."
            ;;
    esac
}


echo "Trying to close Autodarts-desktop"
PID=$(pgrep -f "autodarts-desktop")
if [ -z "$PID" ]; then
    echo "Autodarts-desktop doesn't run."
else
    echo "Close Autodarts-desktop ${PID}'."
    kill $PID
    sleep 2
    if ps -p $PID > /dev/null; then
        kill -9 $PID
    fi
    sleep 2
fi


if [[ $1 == "--uninstall" ]]; then
    echo "Trying to remove Autodarts-desktop"
    rm -rf ~/autodarts-desktop
    remove_from_autostart
    exit
fi

PLATFORM=$(uname)
if [[ "$PLATFORM" = "Linux" ]]; then 
    PLATFORM="linux"
elif [[ "$PLATFORM" = "Darwin" ]]; then
    PLATFORM="macOS"
    sudo spctl --master-disable 
else
    echo "Platform is not 'linux', and hence is not supported by this script." && exit 1
fi

ARCH=$(uname -m)
case "${ARCH}" in
    "x86_64"|"amd64") ARCH="X64";;
    "aarch64"|"arm64") ARCH="ARM64";;
    "armv7l") ARCH="ARM";;
    *) echo "Kernel architecture '${ARCH}' is not supported." && exit 1;;
esac

mkdir -p ~/autodarts-desktop
echo "Downloading and extracting 'autodarts-desktop-${PLATFORM}-${ARCH}.zip' into '~/autodarts-desktop'."  
curl -sL https://github.com/lbormann/autodarts-desktop/releases/latest/download/autodarts-desktop-${PLATFORM}-${ARCH}.zip -o autodarts-desktop.zip && unzip -o autodarts-desktop.zip -d ~/autodarts-desktop && rm autodarts-desktop.zip
echo "Making ~/autodarts-desktop/autodarts-desktop executable."
chmod +x ~/autodarts-desktop/autodarts-desktop
echo "Starting autodarts-desktop."
~/autodarts-desktop/autodarts-desktop &
add_to_autostart
