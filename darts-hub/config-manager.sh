#!/bin/bash

# DartsHub CLI Manager for Linux/macOS
# Configuration Export/Import Tool

# Colors for better visibility
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to display the main menu
show_main_menu() {
    clear
    echo "================================================"
    echo "           DartsHub CLI Manager"
    echo "        Configuration Export/Import Tool"
    echo "================================================"
    echo ""
    echo "What would you like to do?"
    echo ""
    echo "=== Configuration Management ==="
    echo "[1] Export Configuration"
    echo "[2] Import Configuration"
    echo "[3] List Available Extensions"
    echo "[4] List Exports"
    echo "[5] View Export Information"
    echo ""
    echo "=== Backup & Restore ==="
    echo "[6] Create Backup"
    echo "[7] Restore Backup"
    echo "[8] List Backups"
    echo "[9] Cleanup Old Backups"
    echo ""
    echo "=== Information ==="
    echo "[10] Application Info"
    echo "[11] System Info"
    echo "[12] List Profiles"
    echo "[13] Version Info"
    echo ""
    echo "=== Testing ==="
    echo "[14] Updater Tests"
    echo ""
    echo "=== Other ==="
    echo "[15] Help"
    echo "[0]  Exit"
    echo ""
    read -p "Enter your choice (0-15): " choice
    
    case $choice in
        1) export_menu ;;
        2) import_menu ;;
        3) list_extensions ;;
        4) list_exports ;;
        5) view_export_info ;;
        6) backup_menu ;;
        7) restore_backup ;;
        8) list_backups ;;
        9) cleanup_backups ;;
        10) app_info ;;
        11) system_info ;;
        12) list_profiles ;;
        13) version_info ;;
        14) test_menu ;;
        15) show_help ;;
        0) exit_script ;;
        *) 
            echo ""
            echo "Invalid choice. Press any key to continue..."
            read -n 1
            show_main_menu
            ;;
    esac
}

# Export Menu
export_menu() {
    clear
    echo "================================================"
    echo "           Export Configuration"
    echo "================================================"
    echo ""
    echo "What type of export would you like to create?"
    echo ""
    echo "[1] Full Configuration (all extensions)"
    echo "[2] Specific Extension(s)"
    echo "[3] Specific Parameters (interactive)"
    echo "[0] Back to Main Menu"
    echo ""
    read -p "Enter your choice (0-3): " export_choice
    
    case $export_choice in
        1) export_full ;;
        2) export_extensions ;;
        3) export_parameters ;;
        0) show_main_menu ;;
        *)
            echo ""
            echo "Invalid choice. Press any key to continue..."
            read -n 1
            export_menu
            ;;
    esac
}

# Export Full Configuration
export_full() {
    clear
    echo "================================================"
    echo "         Export Full Configuration"
    echo "================================================"
    echo ""
    echo "This will export all extensions with all their settings."
    echo ""
    echo "NOTE: If 'darts-caller' is included, you will be asked"
    echo "      whether to exclude sensitive credentials."
    echo ""
    read -p "Enter export name (press Enter for automatic name): " export_name
    echo ""
    read -p "Enter description (optional): " export_desc
    echo ""
    echo "Exporting..."
    echo ""
    
    if [ -z "$export_name" ]; then
        if [ -z "$export_desc" ]; then
            ./darts-hub --export
        else
            ./darts-hub --export "" "$export_desc"
        fi
    else
        if [ -z "$export_desc" ]; then
            ./darts-hub --export "$export_name"
        else
            ./darts-hub --export "$export_name" "$export_desc"
        fi
    fi
    
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Export Specific Extensions
export_extensions() {
    clear
    echo "================================================"
    echo "       Export Specific Extensions"
    echo "================================================"
    echo ""
    echo "First, let's see available extensions..."
    echo ""
    ./darts-hub --list-extensions
    echo ""
    echo "Enter extension names separated by spaces."
    echo "Example: darts-caller darts-wled darts-pixelit"
    echo ""
    echo "NOTE: If 'darts-caller' is included, you will be asked"
    echo "      whether to exclude sensitive credentials."
    echo ""
    read -p "Enter extension names: " extensions
    
    if [ -z "$extensions" ]; then
        echo ""
        echo "Error: No extensions specified."
        read -p "Press any key to continue..." -n 1
        export_menu
        return
    fi
    
    echo ""
    echo "Exporting extensions: $extensions"
    echo ""
    ./darts-hub --export-ext $extensions
    
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Export Specific Parameters
export_parameters() {
    clear
    echo "================================================"
    echo "       Export Specific Parameters"
    echo "================================================"
    echo ""
    echo "This will start an interactive parameter selection."
    echo "You'll be able to:"
    echo " - Select which extensions to export from"
    echo " - Choose specific parameters from each extension"
    echo " - Only parameters with values will be exported"
    echo ""
    read -p "Press any key to start interactive parameter export..." -n 1
    echo ""
    
    ./darts-hub --export-params
    
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Import Menu
import_menu() {
    clear
    echo "================================================"
    echo "         Import Configuration"
    echo "================================================"
    echo ""
    echo "Available export files:"
    echo ""
    ./darts-hub --list-exports
    echo ""
    echo "================================================"
    echo ""
    echo "Import Options:"
    echo "[1] Select from available exports (merge mode)"
    echo "[2] Select from available exports (replace mode)"
    echo "[3] Specify custom file path"
    echo "[0] Back to Main Menu"
    echo ""
    read -p "Enter your choice (0-3): " import_choice
    
    case $import_choice in
        1) import_select_merge ;;
        2) import_select_replace ;;
        3) import_custom ;;
        0) show_main_menu ;;
        *)
            echo ""
            echo "Invalid choice. Press any key to continue..."
            read -n 1
            import_menu
            ;;
    esac
}

# Import - Select from available (Merge)
import_select_merge() {
    clear
    echo "================================================"
    echo "     Select Export to Import (Merge Mode)"
    echo "================================================"
    echo ""
    echo "Available exports:"
    echo ""
    
    # Get list of export files
    count=0
    declare -a files
    
    if [ -d "exports" ]; then
        for file in exports/*.json; do
            if [ -f "$file" ]; then
                ((count++))
                files[$count]="$file"
                echo "[$count] $(basename "$file")"
            fi
        done
    fi
    
    if [ $count -eq 0 ]; then
        echo "No export files found."
        echo ""
        read -p "Press any key to continue..." -n 1
        import_menu
        return
    fi
    
    echo ""
    read -p "Enter file number: " file_num
    
    if [ -z "$file_num" ] || [ "$file_num" -lt 1 ] || [ "$file_num" -gt $count ]; then
        echo "Invalid file number."
        read -p "Press any key to continue..." -n 1
        import_select_merge
        return
    fi
    
    selected_file="${files[$file_num]}"
    
    echo ""
    echo "Importing: $selected_file"
    echo "Mode: Merge (safe - updates existing, adds new)"
    echo ""
    ./darts-hub --import "$selected_file"
    
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Import - Select from available (Replace)
import_select_replace() {
    clear
    echo "================================================"
    echo "   Select Export to Import (Replace Mode)"
    echo "================================================"
    echo ""
    echo -e "${RED}WARNING: Replace mode will completely overwrite${NC}"
    echo -e "${RED}your current configuration!${NC}"
    echo ""
    echo "Available exports:"
    echo ""
    
    # Get list of export files
    count=0
    declare -a files
    
    if [ -d "exports" ]; then
        for file in exports/*.json; do
            if [ -f "$file" ]; then
                ((count++))
                files[$count]="$file"
                echo "[$count] $(basename "$file")"
            fi
        done
    fi
    
    if [ $count -eq 0 ]; then
        echo "No export files found."
        echo ""
        read -p "Press any key to continue..." -n 1
        import_menu
        return
    fi
    
    echo ""
    read -p "Enter file number: " file_num
    
    if [ -z "$file_num" ] || [ "$file_num" -lt 1 ] || [ "$file_num" -gt $count ]; then
        echo "Invalid file number."
        read -p "Press any key to continue..." -n 1
        import_select_replace
        return
    fi
    
    selected_file="${files[$file_num]}"
    
    echo ""
    echo "Importing: $selected_file"
    echo "Mode: Replace (DANGER - replaces everything!)"
    echo ""
    read -p "Are you sure? Type YES to confirm: " confirm
    
    if [ "$confirm" != "YES" ]; then
        echo "Import cancelled."
        read -p "Press any key to continue..." -n 1
        import_menu
        return
    fi
    
    echo ""
    ./darts-hub --import "$selected_file" replace
    
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Import - Custom path
import_custom() {
    clear
    echo "================================================"
    echo "        Import from Custom Path"
    echo "================================================"
    echo ""
    read -p "Enter full path to export file: " custom_path
    
    if [ ! -f "$custom_path" ]; then
        echo ""
        echo "Error: File not found: $custom_path"
        read -p "Press any key to continue..." -n 1
        import_menu
        return
    fi
    
    echo ""
    echo "Import mode:"
    echo "[1] Merge (safe - updates existing, adds new)"
    echo "[2] Replace (DANGER - replaces everything!)"
    echo ""
    read -p "Enter your choice (1-2): " mode_choice
    
    case $mode_choice in
        1)
            echo ""
            echo "Importing: $custom_path"
            echo "Mode: Merge"
            echo ""
            ./darts-hub --import "$custom_path"
            ;;
        2)
            echo ""
            read -p "WARNING: This will replace everything! Type YES to confirm: " confirm
            if [ "$confirm" = "YES" ]; then
                echo ""
                echo "Importing: $custom_path"
                echo "Mode: Replace"
                echo ""
                ./darts-hub --import "$custom_path" replace
            else
                echo "Import cancelled."
            fi
            ;;
        *)
            echo "Invalid choice. Import cancelled."
            ;;
    esac
    
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# List Extensions
list_extensions() {
    clear
    echo "================================================"
    echo "        Available Extensions"
    echo "================================================"
    echo ""
    ./darts-hub --list-extensions
    echo ""
    echo "================================================"
    echo ""
    echo "[1] Show parameters for an extension"
    echo "[0] Back to Main Menu"
    echo ""
    read -p "Enter your choice (0-1): " ext_choice
    
    case $ext_choice in
        1)
            echo ""
            read -p "Enter extension name: " ext_name
            echo ""
            ./darts-hub --list-params "$ext_name"
            echo ""
            read -p "Press any key to continue..." -n 1
            list_extensions
            ;;
        0)
            show_main_menu
            ;;
        *)
            list_extensions
            ;;
    esac
}

# List Exports
list_exports() {
    clear
    echo "================================================"
    echo "          Available Export Files"
    echo "================================================"
    echo ""
    ./darts-hub --list-exports
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# View Export Info
view_export_info() {
    clear
    echo "================================================"
    echo "       View Export Information"
    echo "================================================"
    echo ""
    echo "Available exports:"
    echo ""
    
    # Get list of export files
    count=0
    declare -a files
    
    if [ -d "exports" ]; then
        for file in exports/*.json; do
            if [ -f "$file" ]; then
                ((count++))
                files[$count]="$file"
                echo "[$count] $(basename "$file")"
            fi
        done
    fi
    
    if [ $count -eq 0 ]; then
        echo "No export files found."
        echo ""
        read -p "Press any key to continue..." -n 1
        show_main_menu
        return
    fi
    
    echo "[0] Back to Main Menu"
    echo ""
    read -p "Enter file number: " file_num
    
    if [ "$file_num" = "0" ]; then
        show_main_menu
        return
    fi
    
    if [ -z "$file_num" ] || [ "$file_num" -lt 1 ] || [ "$file_num" -gt $count ]; then
        echo "Invalid file number."
        read -p "Press any key to continue..." -n 1
        view_export_info
        return
    fi
    
    selected_file="${files[$file_num]}"
    
    echo ""
    ./darts-hub --export-info "$selected_file"
    
    echo ""
    read -p "Press any key to continue..." -n 1
    view_export_info
}

# Backup Menu
backup_menu() {
    clear
    echo "================================================"
    echo "             Backup Manager"
    echo "================================================"
    echo ""
    echo "What would you like to do?"
    echo ""
    echo "[1] Create Backup"
    echo "[2] Restore Backup"
    echo "[3] List Backups"
    echo "[4] Cleanup Old Backups"
    echo "[0] Back to Main Menu"
    echo ""
    read -p "Enter your choice (0-4): " backup_choice
    
    case $backup_choice in
        1) create_backup ;;
        2) restore_backup ;;
        3) list_backups ;;
        4) cleanup_backups ;;
        0) show_main_menu ;;
        *)
            echo ""
            echo "Invalid choice. Press any key to continue..."
            read -n 1
            backup_menu
            ;;
    esac
}

# Create Backup
create_backup() {
    clear
    echo "================================================"
    echo "             Create Backup"
    echo "================================================"
    echo ""
    echo "What type of backup would you like to create?"
    echo ""
    echo "[1] Full Backup (configuration, profiles, logs)"
    echo "[2] Configuration Only Backup"
    echo "[0] Back to Main Menu"
    echo ""
    read -p "Enter your choice (0-2): " backup_type
    
    case $backup_type in
        1) backup_full ;;
        2) backup_config ;;
        0) show_main_menu ;;
        *)
            echo ""
            echo "Invalid choice. Press any key to continue..."
            read -n 1
            create_backup
            ;;
    esac
}

# Full Backup
backup_full() {
    clear
    echo "================================================"
    echo "           Create Full Backup"
    echo "================================================"
    echo ""
    echo "This will backup:"
    echo "  - All configuration files"
    echo "  - Dart profiles"
    echo "  - Log files"
    echo ""
    read -p "Enter backup name (optional, press Enter for automatic): " backup_name
    echo ""
    echo "Creating full backup..."
    echo ""
    
    if [ -z "$backup_name" ]; then
        ./darts-hub --backup
    else
        ./darts-hub --backup "$backup_name"
    fi
    
    echo ""
    echo "Backup created successfully."
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Config Backup
backup_config() {
    clear
    echo "================================================"
    echo "      Create Configuration Backup"
    echo "================================================"
    echo ""
    echo "This will backup configuration files only."
    echo ""
    read -p "Enter backup name (optional, press Enter for automatic): " backup_name
    echo ""
    echo "Creating configuration backup..."
    echo ""
    
    if [ -z "$backup_name" ]; then
        ./darts-hub --backup-config
    else
        ./darts-hub --backup-config "$backup_name"
    fi
    
    echo ""
    echo "Backup created successfully."
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# List Backups
list_backups() {
    clear
    echo "================================================"
    echo "          Available Backups"
    echo "================================================"
    echo ""
    ./darts-hub --backup-list
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Restore Backup
restore_backup() {
    clear
    echo "================================================"
    echo "           Restore Backup"
    echo "================================================"
    echo ""
    echo "Available backups:"
    echo ""
    
    # Get list of backup files
    count=0
    declare -a backups
    
    if [ -d "backups" ]; then
        for file in backups/*.zip; do
            if [ -f "$file" ]; then
                ((count++))
                backups[$count]="$file"
                echo "[$count] $(basename "$file")"
            fi
        done
    fi
    
    if [ $count -eq 0 ]; then
        echo "No backup files found."
        echo ""
        read -p "Press any key to continue..." -n 1
        show_main_menu
        return
    fi
    
    echo "[0] Back to Main Menu"
    echo ""
    read -p "Enter backup number to restore: " backup_num
    
    if [ "$backup_num" = "0" ]; then
        show_main_menu
        return
    fi
    
    if [ -z "$backup_num" ] || [ "$backup_num" -lt 1 ] || [ "$backup_num" -gt $count ]; then
        echo "Invalid backup number."
        read -p "Press any key to continue..." -n 1
        restore_backup
        return
    fi
    
    selected_backup="${backups[$backup_num]}"
    
    echo ""
    echo -e "${YELLOW}WARNING: This will restore your configuration from the backup.${NC}"
    echo -e "${YELLOW}         Current configuration will be backed up first.${NC}"
    echo ""
    read -p "Are you sure? Type YES to confirm: " confirm
    
    if [ "$confirm" != "YES" ]; then
        echo "Restore cancelled."
        read -p "Press any key to continue..." -n 1
        show_main_menu
        return
    fi
    
    echo ""
    echo "Restoring backup: $selected_backup"
    echo ""
    ./darts-hub --backup-restore "$selected_backup"
    
    echo ""
    echo "Backup restored successfully."
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Cleanup Backups
cleanup_backups() {
    clear
    echo "================================================"
    echo "        Cleanup Old Backups"
    echo "================================================"
    echo ""
    echo "This will delete old backup files, keeping only the most recent ones."
    echo ""
    read -p "How many backups to keep? (default: 10): " keep_count
    
    if [ -z "$keep_count" ]; then
        keep_count=10
    fi
    
    echo ""
    echo "Cleaning up backups (keeping $keep_count most recent)..."
    echo ""
    ./darts-hub --backup-cleanup "$keep_count"
    
    echo ""
    echo "Old backups cleaned up successfully."
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Application Info
app_info() {
    clear
    echo "================================================"
    echo "         Application Information"
    echo "================================================"
    echo ""
    ./darts-hub --info
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# System Info
system_info() {
    clear
    echo "================================================"
    echo "          System Information"
    echo "================================================"
    echo ""
    ./darts-hub --system-info
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# List Profiles
list_profiles() {
    clear
    echo "================================================"
    echo "            Dart Profiles"
    echo "================================================"
    echo ""
    ./darts-hub --list-profiles
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Version Info
version_info() {
    clear
    echo "================================================"
    echo "           Version Information"
    echo "================================================"
    echo ""
    ./darts-hub --version
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Test Menu
test_menu() {
    clear
    echo "================================================"
    echo "           Updater Testing"
    echo "================================================"
    echo ""
    echo "Test Options:"
    echo ""
    echo "[1] Interactive Updater Test Menu"
    echo "[2] Run Full Test Suite"
    echo "[3] Test Version Checking"
    echo "[4] Test Retry Mechanism"
    echo "[5] Test Logging System"
    echo "[0] Back to Main Menu"
    echo ""
    read -p "Enter your choice (0-5): " test_choice
    
    case $test_choice in
        1) test_interactive ;;
        2) test_full ;;
        3) test_version ;;
        4) test_retry ;;
        5) test_logging ;;
        0) show_main_menu ;;
        *)
            echo ""
            echo "Invalid choice. Press any key to continue..."
            read -n 1
            test_menu
            ;;
    esac
}

# Test - Interactive
test_interactive() {
    clear
    echo "================================================"
    echo "     Interactive Updater Test Menu"
    echo "================================================"
    echo ""
    echo "Starting interactive test menu..."
    echo ""
    ./darts-hub --test-updater
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Test - Full
test_full() {
    clear
    echo "================================================"
    echo "        Full Updater Test Suite"
    echo "================================================"
    echo ""
    echo "Running comprehensive updater tests..."
    echo "This may take several minutes."
    echo ""
    ./darts-hub --test-full
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Test - Version
test_version() {
    clear
    echo "================================================"
    echo "        Version Checking Test"
    echo "================================================"
    echo ""
    echo "Testing version checking functionality..."
    echo ""
    ./darts-hub --test-version
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Test - Retry
test_retry() {
    clear
    echo "================================================"
    echo "        Retry Mechanism Test"
    echo "================================================"
    echo ""
    echo "Testing retry mechanism..."
    echo ""
    ./darts-hub --test-retry
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Test - Logging
test_logging() {
    clear
    echo "================================================"
    echo "         Logging System Test"
    echo "================================================"
    echo ""
    echo "Testing logging system..."
    echo "Check logs/ directory for output."
    echo ""
    ./darts-hub --test-logging
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Help
show_help() {
    clear
    echo "================================================"
    echo "                 Help"
    echo "================================================"
    echo ""
    echo "DartsHub Configuration Export/Import Tool"
    echo ""
    echo "This tool helps you manage your DartsHub configuration:"
    echo ""
    echo "EXPORT:"
    echo "  - Full Export: Backup all extensions and settings"
    echo "  - Extension Export: Export specific extensions only"
    echo "  - Parameter Export: Export only specific parameter values"
    echo ""
    echo "IMPORT:"
    echo "  - Merge Mode: Safe - updates existing, adds new items"
    echo "  - Replace Mode: DANGER - completely replaces configuration"
    echo ""
    echo "BACKUP & RESTORE:"
    echo "  - Full Backup: Complete system backup (config, profiles, logs)"
    echo "  - Config Backup: Configuration files only"
    echo "  - Restore: Restore from previous backup"
    echo "  - Cleanup: Remove old backups"
    echo ""
    echo "INFORMATION:"
    echo "  - Application Info: Shows DartsHub version and details"
    echo "  - System Info: Shows system and environment information"
    echo "  - List Profiles: Shows all dart profiles"
    echo "  - Version Info: Shows current version"
    echo ""
    echo "TESTING:"
    echo "  - Updater Tests: Test update functionality"
    echo "  - Version Check: Test version checking"
    echo "  - Retry Tests: Test retry mechanism"
    echo "  - Logging Tests: Test logging system"
    echo ""
    echo "FEATURES:"
    echo "  - Automatic backups before any import"
    echo "  - Only exports parameters with values"
    echo "  - Smart import: only updates changed values"
    echo "  - Interactive parameter selection"
    echo "  - Credentials protection for darts-caller"
    echo ""
    echo "SAFETY:"
    echo "  - All operations create automatic backups"
    echo "  - Backups stored in: backups/config-backups/"
    echo "  - Use Merge mode for safe updates"
    echo "  - Check export info before importing"
    echo ""
    echo "TIPS:"
    echo "  - Create regular backups with Full Export"
    echo "  - Use Parameter Export to share specific settings"
    echo "  - Always check export info before importing"
    echo "  - Keep important exports in a safe location"
    echo ""
    echo "For more information, see:"
    echo "  docs/config-export-import.md"
    echo "  docs/command-line-interface.md"
    echo ""
    read -p "Press any key to continue..." -n 1
    show_main_menu
}

# Exit
exit_script() {
    clear
    echo "================================================"
    echo "        Thank you for using DartsHub!"
    echo "================================================"
    echo ""
    echo "Goodbye!"
    echo ""
    sleep 2
    exit 0
}

# Main entry point
main() {
    # Check if darts-hub executable exists
    if [ ! -f "./darts-hub" ]; then
        echo -e "${RED}Error: darts-hub executable not found in current directory.${NC}"
        echo "Please run this script from the DartsHub installation directory."
        exit 1
    fi
    
    # Make sure darts-hub is executable
    chmod +x ./darts-hub 2>/dev/null
    
    # Start the main menu
    show_main_menu
}

# Run the main function
main
