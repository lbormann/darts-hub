@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul 2>&1
color 0A

:MAIN_MENU
cls
echo ================================================
echo            DartsHub CLI Manager
echo         Configuration Export/Import Tool
echo ================================================
echo.
echo What would you like to do?
echo.
echo === Configuration Management ===
echo [1] Export Configuration
echo [2] Import Configuration
echo [3] List Available Extensions
echo [4] List Exports
echo [5] View Export Information
echo.
echo === Backup ^& Restore ===
echo [6] Create Backup
echo [7] Restore Backup
echo [8] List Backups
echo [9] Cleanup Old Backups
echo.
echo === Information ===
echo [10] Application Info
echo [11] System Info
echo [12] List Profiles
echo [13] Version Info
echo.
echo === Testing ===
echo [14] Updater Tests
echo.
echo === Other ===
echo [15] Help
echo [0]  Exit
echo.
set /p choice="Enter your choice (0-15): "

if "%choice%"=="1" goto EXPORT_MENU
if "%choice%"=="2" goto IMPORT_MENU
if "%choice%"=="3" goto LIST_EXTENSIONS
if "%choice%"=="4" goto LIST_EXPORTS
if "%choice%"=="5" goto VIEW_EXPORT_INFO
if "%choice%"=="6" goto BACKUP_MENU
if "%choice%"=="7" goto RESTORE_BACKUP
if "%choice%"=="8" goto LIST_BACKUPS
if "%choice%"=="9" goto CLEANUP_BACKUPS
if "%choice%"=="10" goto APP_INFO
if "%choice%"=="11" goto SYSTEM_INFO
if "%choice%"=="12" goto LIST_PROFILES
if "%choice%"=="13" goto VERSION_INFO
if "%choice%"=="14" goto TEST_MENU
if "%choice%"=="15" goto SHOW_HELP
if "%choice%"=="0" goto EXIT
echo.
echo Invalid choice. Press any key to continue...
pause >nul
goto MAIN_MENU

:EXPORT_MENU
cls
echo ================================================
echo            Export Configuration
echo ================================================
echo.
echo What type of export would you like to create?
echo.
echo [1] Full Configuration (all extensions)
echo [2] Specific Extension(s)
echo [3] Specific Parameters (interactive)
echo [0] Back to Main Menu
echo.
set /p export_choice="Enter your choice (0-3): "

if "%export_choice%"=="1" goto EXPORT_FULL
if "%export_choice%"=="2" goto EXPORT_EXTENSIONS
if "%export_choice%"=="3" goto EXPORT_PARAMETERS
if "%export_choice%"=="0" goto MAIN_MENU
echo.
echo Invalid choice. Press any key to continue...
pause >nul
goto EXPORT_MENU

:EXPORT_FULL
cls
echo ================================================
echo          Export Full Configuration
echo ================================================
echo.
echo This will export all extensions with all their settings.
echo.
echo NOTE: If 'darts-caller' is included, you will be asked
echo       whether to exclude sensitive credentials.
echo.
set /p export_name="Enter export name (press Enter for automatic name): "
echo.
set /p export_desc="Enter description (optional): "
echo.
echo Exporting...
echo.

if "%export_name%"=="" (
    if "%export_desc%"=="" (
        darts-hub.exe --export
    ) else (
        darts-hub.exe --export "" "%export_desc%"
    )
) else (
    if "%export_desc%"=="" (
        darts-hub.exe --export "%export_name%"
    ) else (
        darts-hub.exe --export "%export_name%" "%export_desc%"
    )
)

echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:EXPORT_EXTENSIONS
cls
echo ================================================
echo        Export Specific Extensions
echo ================================================
echo.
echo First, let's see available extensions...
echo.
darts-hub.exe --list-extensions
echo.
echo Enter extension names separated by spaces.
echo Example: darts-caller darts-wled darts-pixelit
echo.
echo NOTE: If 'darts-caller' is included, you will be asked
echo       whether to exclude sensitive credentials.
echo.
set /p extensions="Enter extension names: "

if "%extensions%"=="" (
    echo.
    echo Error: No extensions specified.
    echo Press any key to continue...
    pause >nul
    goto EXPORT_MENU
)

echo.
echo Exporting extensions: %extensions%
echo.
darts-hub.exe --export-ext %extensions%

echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:EXPORT_PARAMETERS
cls
echo ================================================
echo        Export Specific Parameters
echo ================================================
echo.
echo This will open an interactive parameter selection window.
echo You'll be able to:
echo  - Select which extensions to export from
echo  - Choose specific parameters from each extension
echo  - Only parameters with values will be exported
echo.
echo Press any key to start interactive parameter export...
pause >nul

darts-hub.exe --export-params

echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:IMPORT_MENU
cls
echo ================================================
echo          Import Configuration
echo ================================================
echo.
echo Available export files:
echo.
darts-hub.exe --list-exports
echo.
echo ================================================
echo.
echo Import Options:
echo [1] Select from available exports (merge mode)
echo [2] Select from available exports (replace mode)
echo [3] Specify custom file path
echo [0] Back to Main Menu
echo.
set /p import_choice="Enter your choice (0-3): "

if "%import_choice%"=="1" goto IMPORT_SELECT_MERGE
if "%import_choice%"=="2" goto IMPORT_SELECT_REPLACE
if "%import_choice%"=="3" goto IMPORT_CUSTOM
if "%import_choice%"=="0" goto MAIN_MENU
echo.
echo Invalid choice. Press any key to continue...
pause >nul
goto IMPORT_MENU

:IMPORT_SELECT_MERGE
cls
echo ================================================
echo      Select Export to Import (Merge Mode)
echo ================================================
echo.
echo Available exports:
echo.

REM Get list of export files
set count=0
for %%f in (exports\*.json) do (
    set /a count+=1
    set "file!count!=%%f"
    echo [!count!] %%~nxf
)

if %count%==0 (
    echo No export files found.
    echo.
    echo Press any key to continue...
    pause >nul
    goto IMPORT_MENU
)

echo.
set /p file_num="Enter file number: "

if not defined file%file_num% (
    echo Invalid file number.
    echo Press any key to continue...
    pause >nul
    goto IMPORT_SELECT_MERGE
)

echo.
echo Importing: !file%file_num%!
echo Mode: Merge (safe - updates existing, adds new)
echo.
darts-hub.exe --import "!file%file_num%!"

echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:IMPORT_SELECT_REPLACE
cls
echo ================================================
echo    Select Export to Import (Replace Mode)
echo ================================================
echo.
echo WARNING: Replace mode will completely overwrite
echo your current configuration!
echo.
echo Available exports:
echo.

REM Get list of export files
set count=0
for %%f in (exports\*.json) do (
    set /a count+=1
    set "file!count!=%%f"
    echo [!count!] %%~nxf
)

if %count%==0 (
    echo No export files found.
    echo.
    echo Press any key to continue...
    pause >nul
    goto IMPORT_MENU
)

echo.
set /p file_num="Enter file number: "

if not defined file%file_num% (
    echo Invalid file number.
    echo Press any key to continue...
    pause >nul
    goto IMPORT_SELECT_REPLACE
)

echo.
echo Importing: !file%file_num%!
echo Mode: Replace (DANGER - replaces everything!)
echo.
set /p confirm="Are you sure? Type YES to confirm: "

if not "%confirm%"=="YES" (
    echo Import cancelled.
    echo Press any key to continue...
    pause >nul
    goto IMPORT_MENU
)

echo.
darts-hub.exe --import "!file%file_num%!" replace

echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:IMPORT_CUSTOM
cls
echo ================================================
echo         Import from Custom Path
echo ================================================
echo.
set /p custom_path="Enter full path to export file: "

if not exist "%custom_path%" (
    echo.
    echo Error: File not found: %custom_path%
    echo Press any key to continue...
    pause >nul
    goto IMPORT_MENU
)

echo.
echo Import mode:
echo [1] Merge (safe - updates existing, adds new)
echo [2] Replace (DANGER - replaces everything!)
echo.
set /p mode_choice="Enter your choice (1-2): "

if "%mode_choice%"=="1" (
    echo.
    echo Importing: %custom_path%
    echo Mode: Merge
    echo.
    darts-hub.exe --import "%custom_path%"
) else if "%mode_choice%"=="2" (
    echo.
    set /p confirm="WARNING: This will replace everything! Type YES to confirm: "
    if "!confirm!"=="YES" (
        echo.
        echo Importing: %custom_path%
        echo Mode: Replace
        echo.
        darts-hub.exe --import "%custom_path%" replace
    ) else (
        echo Import cancelled.
    )
) else (
    echo Invalid choice. Import cancelled.
)

echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:LIST_EXTENSIONS
cls
echo ================================================
echo         Available Extensions
echo ================================================
echo.
darts-hub.exe --list-extensions
echo.
echo ================================================
echo.
echo [1] Show parameters for an extension
echo [0] Back to Main Menu
echo.
set /p ext_choice="Enter your choice (0-1): "

if "%ext_choice%"=="1" (
    echo.
    set /p ext_name="Enter extension name: "
    echo.
    darts-hub.exe --list-params "!ext_name!"
    echo.
    echo Press any key to continue...
    pause >nul
    goto LIST_EXTENSIONS
)

if "%ext_choice%"=="0" goto MAIN_MENU
goto LIST_EXTENSIONS

:LIST_EXPORTS
cls
echo ================================================
echo           Available Export Files
echo ================================================
echo.
darts-hub.exe --list-exports
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:VIEW_EXPORT_INFO
cls
echo ================================================
echo        View Export Information
echo ================================================
echo.
echo Available exports:
echo.

REM Get list of export files
set count=0
for %%f in (exports\*.json) do (
    set /a count+=1
    set "file!count!=%%f"
    echo [!count!] %%~nxf
)

if %count%==0 (
    echo No export files found.
    echo.
    echo Press any key to continue...
    pause >nul
    goto MAIN_MENU
)

echo [0] Back to Main Menu
echo.
set /p file_num="Enter file number: "

if "%file_num%"=="0" goto MAIN_MENU

if not defined file%file_num% (
    echo Invalid file number.
    echo Press any key to continue...
    pause >nul
    goto VIEW_EXPORT_INFO
)

echo.
darts-hub.exe --export-info "!file%file_num%!"

echo.
echo Press any key to continue...
pause >nul
goto VIEW_EXPORT_INFO

:BACKUP_MENU
cls
echo ================================================
echo              Backup Manager
echo ================================================
echo.
echo What would you like to do?
echo.
echo [1] Create Backup
echo [2] Restore Backup
echo [3] List Backups
echo [4] Cleanup Old Backups
echo [0] Back to Main Menu
echo.
set /p backup_choice="Enter your choice (0-4): "

if "%backup_choice%"=="1" goto CREATE_BACKUP
if "%backup_choice%"=="2" goto RESTORE_BACKUP
if "%backup_choice%"=="3" goto LIST_BACKUPS
if "%backup_choice%"=="4" goto CLEANUP_BACKUPS
if "%backup_choice%"=="0" goto MAIN_MENU
echo.
echo Invalid choice. Press any key to continue...
pause >nul
goto BACKUP_MENU

:CREATE_BACKUP
cls
echo ================================================
echo              Create Backup
echo ================================================
echo.
echo What type of backup would you like to create?
echo.
echo [1] Full Backup (configuration, profiles, logs)
echo [2] Configuration Only Backup
echo [0] Back to Main Menu
echo.
set /p backup_choice="Enter your choice (0-2): "

if "%backup_choice%"=="1" goto BACKUP_FULL
if "%backup_choice%"=="2" goto BACKUP_CONFIG
if "%backup_choice%"=="0" goto MAIN_MENU
echo.
echo Invalid choice. Press any key to continue...
pause >nul
goto CREATE_BACKUP

:BACKUP_FULL
cls
echo ================================================
echo            Create Full Backup
echo ================================================
echo.
echo This will backup:
echo   - All configuration files
echo   - Dart profiles
echo   - Log files
echo.
set /p backup_name="Enter backup name (optional, press Enter for automatic): "
echo.
echo Creating full backup...
echo.

if "%backup_name%"=="" (
    darts-hub.exe --backup
) else (
    darts-hub.exe --backup "%backup_name%"
)

echo.
echo Backup created successfully.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:BACKUP_CONFIG
cls
echo ================================================
echo       Create Configuration Backup
echo ================================================
echo.
echo This will backup configuration files only.
echo.
set /p backup_name="Enter backup name (optional, press Enter for automatic): "
echo.
echo Creating configuration backup...
echo.

if "%backup_name%"=="" (
    darts-hub.exe --backup-config
) else (
    darts-hub.exe --backup-config "%backup_name%"
)

echo.
echo Backup created successfully.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:LIST_BACKUPS
cls
echo ================================================
echo           Available Backups
echo ================================================
echo.
darts-hub.exe --backup-list
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:RESTORE_BACKUP
cls
echo ================================================
echo            Restore Backup
echo ================================================
echo.
echo Available backups:
echo.

REM Get list of backup files
set count=0
for %%f in (backups\*.zip) do (
    set /a count+=1
    set "backup!count!=%%f"
    echo [!count!] %%~nxf
)

if %count%==0 (
    echo No backup files found.
    echo.
    echo Press any key to continue...
    pause >nul
    goto MAIN_MENU
)

echo [0] Back to Main Menu
echo.
set /p backup_num="Enter backup number to restore: "

if "%backup_num%"=="0" goto MAIN_MENU

if not defined backup%backup_num% (
    echo Invalid backup number.
    echo Press any key to continue...
    pause >nul
    goto RESTORE_BACKUP
)

echo.
echo WARNING: This will restore your configuration from the backup.
echo          Current configuration will be backed up first.
echo.
set /p confirm="Are you sure? Type YES to confirm: "

if not "%confirm%"=="YES" (
    echo Restore cancelled.
    echo Press any key to continue...
    pause >nul
    goto MAIN_MENU
)

echo.
echo Restoring backup: !backup%backup_num%!
echo.
darts-hub.exe --backup-restore "!backup%backup_num%!"

echo.
echo Backup restored successfully.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:CLEANUP_BACKUPS
cls
echo ================================================
echo         Cleanup Old Backups
echo ================================================
echo.
echo This will delete old backup files, keeping only the most recent ones.
echo.
set /p keep_count="How many backups to keep? (default: 10): "

if "%keep_count%"=="" set keep_count=10

echo.
echo Cleaning up backups (keeping %keep_count% most recent)...
echo.
darts-hub.exe --backup-cleanup %keep_count%

echo.
echo Old backups cleaned up successfully.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

REM ============================================
REM INFORMATION SECTION
REM ============================================

:APP_INFO
cls
echo ================================================
echo          Application Information
echo ================================================
echo.
darts-hub.exe --info
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:SYSTEM_INFO
cls
echo ================================================
echo           System Information
echo ================================================
echo.
darts-hub.exe --system-info
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:LIST_PROFILES
cls
echo ================================================
echo             Dart Profiles
echo ================================================
echo.
darts-hub.exe --list-profiles
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:VERSION_INFO
cls
echo ================================================
echo            Version Information
echo ================================================
echo.
darts-hub.exe --version
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

REM ============================================
REM TESTING SECTION
REM ============================================

:TEST_MENU
cls
echo ================================================
echo            Updater Testing
echo ================================================
echo.
echo Test Options:
echo.
echo [1] Interactive Updater Test Menu
echo [2] Run Full Test Suite
echo [3] Test Version Checking
echo [4] Test Retry Mechanism
echo [5] Test Logging System
echo [0] Back to Main Menu
echo.
set /p test_choice="Enter your choice (0-5): "

if "%test_choice%"=="1" goto TEST_INTERACTIVE
if "%test_choice%"=="2" goto TEST_FULL
if "%test_choice%"=="3" goto TEST_VERSION
if "%test_choice%"=="4" goto TEST_RETRY
if "%test_choice%"=="5" goto TEST_LOGGING
if "%test_choice%"=="0" goto MAIN_MENU
echo.
echo Invalid choice. Press any key to continue...
pause >nul
goto TEST_MENU

:TEST_INTERACTIVE
cls
echo ================================================
echo      Interactive Updater Test Menu
echo ================================================
echo.
echo Starting interactive test menu...
echo.
darts-hub.exe --test-updater
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:TEST_FULL
cls
echo ================================================
echo         Full Updater Test Suite
echo ================================================
echo.
echo Running comprehensive updater tests...
echo This may take several minutes.
echo.
darts-hub.exe --test-full
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:TEST_VERSION
cls
echo ================================================
echo         Version Checking Test
echo ================================================
echo.
echo Testing version checking functionality...
echo.
darts-hub.exe --test-version
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:TEST_RETRY
cls
echo ================================================
echo         Retry Mechanism Test
echo ================================================
echo.
echo Testing retry mechanism...
echo.
darts-hub.exe --test-retry
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:TEST_LOGGING
cls
echo ================================================
echo          Logging System Test
echo ================================================
echo.
echo Testing logging system...
echo Check logs/ directory for output.
echo.
darts-hub.exe --test-logging
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:SHOW_HELP
cls
echo ================================================
echo                  Help
echo ================================================
echo.
echo DartsHub Configuration Export/Import Tool
echo.
echo This tool helps you manage your DartsHub configuration:
echo.
echo EXPORT:
echo   - Full Export: Backup all extensions and settings
echo   - Extension Export: Export specific extensions only
echo   - Parameter Export: Export only specific parameter values
echo.
echo IMPORT:
echo   - Merge Mode: Safe - updates existing, adds new items
echo   - Replace Mode: DANGER - completely replaces configuration
echo.
echo BACKUP ^& RESTORE:
echo   - Full Backup: Complete system backup (config, profiles, logs)
echo   - Config Backup: Configuration files only
echo   - Restore: Restore from previous backup
echo   - Cleanup: Remove old backups
echo.
echo INFORMATION:
echo   - Application Info: Shows DartsHub version and details
echo   - System Info: Shows system and environment information
echo   - List Profiles: Shows all dart profiles
echo   - Version Info: Shows current version
echo.
echo TESTING:
echo   - Updater Tests: Test update functionality
echo   - Version Check: Test version checking
echo   - Retry Tests: Test retry mechanism
echo   - Logging Tests: Test logging system
echo.
echo FEATURES:
echo   - Automatic backups before any import
echo   - Only exports parameters with values
echo   - Smart import: only updates changed values
echo   - Interactive parameter selection
echo   - Credentials protection for darts-caller
echo.
echo SAFETY:
echo   - All operations create automatic backups
echo   - Backups stored in: backups/config-backups/
echo   - Use Merge mode for safe updates
echo   - Check export info before importing
echo.
echo TIPS:
echo   - Create regular backups with Full Export
echo   - Use Parameter Export to share specific settings
echo   - Always check export info before importing
echo   - Keep important exports in a safe location
echo.
echo For more information, see:
echo   docs/config-export-import.md
echo   docs/command-line-interface.md
echo.
echo Press any key to continue...
pause >nul
goto MAIN_MENU

:EXIT
cls
echo ================================================
echo         Thank you for using DartsHub!
echo ================================================
echo.
echo Goodbye!
echo.
timeout /t 2 >nul
exit /b 0
