#!/bin/bash

add_to_autostart() {
    echo "Trying to add darts-hub to autostart."

    case "$PLATFORM" in
        "linux")
            mkdir -p "${HOME}/.config/autostart"
            echo "[Desktop Entry]
            Type=Application
            Name=Darts-hub
            Exec="${HOME}/darts-hub/darts-hub"
            Terminal=false
            Hidden=false
            NoDisplay=false
            X-GNOME-Autostart-enabled=true
            X-GNOME-Autostart-Delay=10" > "${HOME}/.config/autostart/darts-hub.desktop"
            ;;
        "macOS")
            osascript -e "tell application \"System Events\" to make new login item at end with properties {path:\"${HOME}/darts-hub/darts-hub\", hidden:false}" > /dev/null
            ;;
        *)
            echo "Platform is not 'linux' or 'macOS', and hence autostart configuration is not supported by this script."
            ;;
    esac
}

remove_from_autostart() {
    echo "Trying to remove darts-hub from autostart."
    case "$PLATFORM" in
        "linux")
            if [[ -f "${HOME}/.config/autostart/darts-hub.desktop" ]]; then
                rm "${HOME}/.config/autostart/darts-hub.desktop"
            else
                echo "darts-hub is not in autostart. Nothing to remove."
            fi
            ;;
        "macOS")
            osascript -e "tell application \"System Events\" to delete login item \"${HOME}/darts-hub/darts-hub\"" > /dev/null
            ;;
        *)
            echo "Platform is not 'linux' or 'macOS', and hence autostart configuration is not supported by this script."
            ;;
    esac
}


echo "Trying to close darts-hub"
PID=$(pgrep -f "darts-hub")
if [ -z "$PID" ]; then
    echo "darts-hub doesn't run."
else
    echo "Close darts-hub ${PID}'."
    kill $PID
    sleep 2
    if ps -p $PID > /dev/null; then
        kill -9 $PID
    fi
    sleep 2
fi


if [[ $1 == "--uninstall" ]]; then
    echo "Trying to remove darts-hub"
    rm -rf ~/darts-hub
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

mkdir -p ~/darts-hub
echo "Downloading and extracting 'darts-hub-${PLATFORM}-${ARCH}.zip' into '~/darts-hub'."  
curl -sL https://github.com/lbormann/darts-hub/releases/latest/download/darts-hub-${PLATFORM}-${ARCH}.zip -o darts-hub.zip && unzip -o darts-hub.zip -d ~/darts-hub && rm darts-hub.zip
echo "Making ~/darts-hub/darts-hub executable."
chmod +x ~/darts-hub/darts-hub
echo "Starting darts-hub."
~/darts-hub/darts-hub &
add_to_autostart
