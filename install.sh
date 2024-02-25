#!/bin/bash

# https://raw.githubusercontent.com/autodarts-desktop/releases/main/install.sh


#!/bin/bash
# cd
# wget -N -P Downloads https://github.com/lbormann/autodarts-desktop/releases/latest/download/autodarts-desktop-linux-X64.zip
# mkdir -p autodarts-desktop
# chmod -R 777 autodarts-desktop
# unzip -f Downloads/autodarts-desktop-linux-X64.zip -d autodarts-desktop
# chmod +x autodarts-desktop/autodarts-desktop
# systemctl disable autodarts.service
# ./autodarts-desktop/autodarts-desktop


if [[ $1 == "--uninstall" ]]; then
    echo "Trying to remove autodarts-desktop"
    
    # exit running process
    PID=$(pgrep -f "autodarts-desktop")
    if [ -z "$PID" ]; then
        echo "Process doesn't run."
    else
        kill $PID
        sleep 1
        if ps -p $PID > /dev/null; then
            kill -9 $PID
        fi
    fi

    # remove folders
    rm ~/autodarts-desktop
    exit
fi


PLATFORM=$(uname)
if [[ "$PLATFORM" = "Linux" ]]; then 
    PLATFORM="linux"
elif [[ "$PLATFORM" = "Darwin" ]]; then
    PLATFORM="macOS"
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

# REQ_VERSION=$1
# REQ_VERSION="${REQ_VERSION#v}"
# if [[ "$REQ_VERSION" = "" ]]; then
#     VERSION=$(curl -sL https://api.github.com/repos/lbormann/autodarts-desktop/releases/latest | grep tag_name | grep -o '[0-9]\+\.[0-9]\+\.[0-9]\+')
#     echo "Installing latest version v${VERSION}."
# else
#     VERSION=$(curl -sL https://api.github.com/repos/lbormann/autodarts-desktop/releases | grep tag_name | grep ${REQ_VERSION} | grep -o '[0-9]\+\.[0-9]\+\.[0-9]\+\(-\(beta\|rc\)[0-9]\+\)\?' | head -1)
#     if [[ "$VERSION" = "" ]]; then
#         echo "Requested version v${REQ_VERSION} not found." && exit 1
#     fi
#     echo "Installing requested version v${VERSION}."
# fi

# Download autodarts-desktop binary and unpack to ~/autodarts-desktop
mkdir -p ~/autodarts-desktop
# autodarts0.23.2.linux-amd64.tar.gz
# autodarts-desktop-linux-X64.zip
echo "Downloading and extracting 'autodarts-desktop-${PLATFORM}-${ARCH}.zip' into '~/autodarts-desktop'."  
curl -sL https://github.com/lbormann/autodarts-desktop/releases/latest/download/autodarts-desktop-${PLATFORM}-${ARCH}.zip -o autodarts-desktop.zip && unzip -o autodarts-desktop.zip -d ~/autodarts-desktop && rm autodarts-desktop.zip
echo "Making ~/autodarts-desktop/autodarts-desktop executable."
chmod +x ~/autodarts-desktop/autodarts-desktop
systemctl disable autodarts.service
~/autodarts-desktop/autodarts-desktop

