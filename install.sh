#!/bin/bash


if [[ $1 == "--uninstall" ]]; then
    echo "Trying to remove Autodarts-desktop"
    
    PID=$(pgrep -f "autodarts-desktop")
    if [ -z "$PID" ]; then
        echo "Autodarts-desktop doesn't run."
    else
        echo "Close Autodarts-desktop ${PID}'."
        kill $PID
        sleep 1
        if ps -p $PID > /dev/null; then
            kill -9 $PID
        fi
    fi

    rm ~/autodarts-desktop
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
echo "Disable autodarts.service."
systemctl disable autodarts.service
echo "Starting autodarts-desktop."
~/autodarts-desktop/autodarts-desktop

