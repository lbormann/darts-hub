#!/bin/bash


PLATFORM=$(uname)
if [[ "$PLATFORM" = "Linux" ]]; then 
    PLATFORM="linux"
elif [[ "$PLATFORM" = "Darwin" ]]; then
    PLATFORM="macOS"
    sudo spctl --master-disable 
else
    echo "Platform '${PLATFORM}' is not supported." && exit 1
fi

ARCH=$(uname -m)
case "${ARCH}" in
    "x86_64"|"amd64") ARCH="X64";;
    "aarch64"|"arm64") ARCH="ARM64";;
    "armv7l") ARCH="ARM";;
    *) echo "Kernel architecture '${ARCH}' is not supported." && exit 1;;
esac


find_darts_hub_installations() {
    local installations=()
    
    # Suche in häufigen Installationsverzeichnissen
    local search_paths=(
        "$HOME"
        "$HOME/bin"
        "$HOME/.local/bin"
        "$HOME/.local/share"
        "$HOME/Applications"
        "$HOME/Downloads"
        "$HOME/Desktop"
        "$HOME/Documents"
        "/opt"
        "/usr/local"
        "/usr/local/bin"
        "/usr/local/share"
        "/usr/bin"
        "/usr/share"
        "/Applications"
        "/snap"
        "/var/lib/snapd/snap"
        "/flatpak"
        "/var/lib/flatpak"
        "/tmp"
        "/var/tmp"
        "/srv"
        "/home"
        "/media"
        "/mnt"
    )
    
    for path in "${search_paths[@]}"; do
        if [[ -d "$path" ]]; then
            # Suche nach darts-hub Verzeichnissen
            while IFS= read -r -d '' dir; do
                # Überprüfe ob es sich um eine gültige darts-hub Installation handelt
                if [[ -f "$dir/darts-hub" || -f "$dir/darts-hub.exe" ]]; then
                    installations+=("$dir")
                fi
            done < <(find "$path" -maxdepth 3 -name "darts-hub" -type d 2>/dev/null -print0)
        fi
    done
    
    # Entferne Duplikate und gebe nur die Pfade zurück
    printf '%s\n' "${installations[@]}" | sort -u
}

select_installation_path() {
    echo "Searching for existing darts-hub installations..."
    
    # Sammle alle Installationen in einem Array
    local installations=()
    while IFS= read -r line; do
        if [[ -n "$line" ]]; then
            installations+=("$line")
        fi
    done < <(find_darts_hub_installations)
    
    if [[ ${#installations[@]} -eq 0 ]]; then
        echo "No existing darts-hub installations found."
        return 1
    elif [[ ${#installations[@]} -eq 1 ]]; then
        echo "Found one darts-hub installation: ${installations[0]}"
        SELECTED_PATH="${installations[0]}"
        return 0
    else
        echo "Multiple darts-hub installations found:"
        for i in "${!installations[@]}"; do
            echo "$((i+1))) ${installations[$i]}"
        done
        
        while true; do
            read -p "Please select which installation to update (1-${#installations[@]}): " choice
            if [[ "$choice" =~ ^[0-9]+$ ]] && [[ "$choice" -ge 1 ]] && [[ "$choice" -le ${#installations[@]} ]]; then
                SELECTED_PATH="${installations[$((choice-1))]}"
                echo "Selected: $SELECTED_PATH"
                return 0
            else
                echo "Invalid selection. Please enter a number between 1 and ${#installations[@]}."
            fi
        done
    fi
}

stop_darts_hub_in_path() {
    local path="$1"
    echo "Trying to close darts-hub in $path"
    
    # Finde alle laufenden darts-hub Prozesse
    local pids=$(pgrep -f "darts-hub")
    
    if [[ -n "$pids" ]]; then
        for pid in $pids; do
            local exe_path=$(ps -p $pid -o args= 2>/dev/null | awk '{print $1}')
            if [[ "$exe_path" == "$path/"* ]]; then
                echo "Stopping darts-hub process $pid from path $path"
                kill $pid
                sleep 2
                if ps -p $pid > /dev/null 2>&1; then
                    echo "Force killing process $pid"
                    kill -9 $pid
                fi
                sleep 1
            fi
        done
    else
        echo "No running darts-hub processes found."
    fi
}

update_darts_hub() {
    if ! select_installation_path; then
        echo "No installation path selected. Exiting."
        exit 1
    fi
    
    echo "Updating darts-hub in: $SELECTED_PATH"
    
    # Stoppe darts-hub in dem gewählten Pfad
    stop_darts_hub_in_path "$SELECTED_PATH"
    
    # Erstelle Backup
    local backup_dir="${SELECTED_PATH}_backup_$(date +%Y%m%d_%H%M%S)"
    echo "Creating backup: $backup_dir"
    cp -r "$SELECTED_PATH" "$backup_dir"
    
    # Download neuer Version
    echo "Downloading latest darts-hub-${PLATFORM}-${ARCH}.zip"
    local temp_zip="/tmp/darts-hub-update.zip"
    local temp_dir="/tmp/darts-hub-update"
    
    if curl -sL "https://github.com/lbormann/darts-hub/releases/latest/download/darts-hub-${PLATFORM}-${ARCH}.zip" -o "$temp_zip"; then
        echo "Download successful."
    else
        echo "Download failed. Aborting update."
        rm -f "$temp_zip"
        exit 1
    fi
    
    # Entpacke in temporäres Verzeichnis
    mkdir -p "$temp_dir"
    if unzip -o "$temp_zip" -d "$temp_dir" > /dev/null; then
        echo "Extraction successful."
    else
        echo "Extraction failed. Aborting update."
        rm -rf "$temp_dir" "$temp_zip"
        exit 1
    fi
    
    # Ersetze Dateien
    echo "Updating files in $SELECTED_PATH"
    cp -rf "$temp_dir"/* "$SELECTED_PATH/"
    
    # Setze Berechtigungen
    echo "Setting permissions."
    chmod +x "$SELECTED_PATH/darts-hub"
    
    # Aufräumen
    rm -rf "$temp_dir" "$temp_zip"
    
    echo "Update completed successfully!"
    echo "Backup created at: $backup_dir"
    
    # Starte darts-hub wieder
    echo "Starting updated darts-hub."
    "$SELECTED_PATH/darts-hub" &
    
    return 0
}



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


# Kommandozeilen-Parameter verarbeiten
case "$1" in
    "--uninstall")
        echo "Trying to remove darts-hub"
        rm -rf ~/darts-hub
        remove_from_autostart
        exit
        ;;
    "--update")
        update_darts_hub
        exit
        ;;
    "--help"|"-h")
        echo "Usage: $0 [OPTIONS]"
        echo ""
        echo "OPTIONS:"
        echo "  --install     Install darts-hub (default behavior)"
        echo "  --update      Update existing darts-hub installation"
        echo "  --uninstall   Remove darts-hub"
        echo "  --help, -h    Show this help message"
        exit
        ;;
    ""|"--install")
        # Standardverhalten: Installation
        ;;
    *)
        echo "Unknown option: $1"
        echo "Use --help for usage information."
        exit 1
        ;;
esac

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

mkdir -p ~/darts-hub
echo "Downloading and extracting 'darts-hub-${PLATFORM}-${ARCH}.zip' into '~/darts-hub'."  
curl -sL https://github.com/lbormann/darts-hub/releases/latest/download/darts-hub-${PLATFORM}-${ARCH}.zip -o darts-hub.zip && unzip -o darts-hub.zip -d ~/darts-hub && rm darts-hub.zip
echo "Making ~/darts-hub/darts-hub executable."
chmod +x ~/darts-hub/darts-hub
echo "Starting darts-hub."
~/darts-hub/darts-hub &
add_to_autostart
