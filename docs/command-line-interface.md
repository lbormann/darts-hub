# DartsHub Command Line Interface (CLI)

DartsHub supports various command line commands for testing, debugging, backup/restore, and system management.

## Overview

```bash
# General usage
darts-hub [COMMAND] [OPTIONS]

# Examples
darts-hub --help
darts-hub --version
darts-hub --test-full
darts-hub --backup my-backup
darts-hub --backup-list
darts-hub --list-profiles
```

## Available Commands

### General Information

| Command | Short | Description |
|---------|-------|-------------|
| `--help` | `-h` | Shows help information |
| `--version` | `-v` | Shows version information (from Updater.cs) |
| `--info` | | Shows detailed application information |
| `--system-info` | `--sysinfo` | Shows system and environment information |

### Profile Management // NOT FINALLY IMPLEMENTED YET

| Command | Alias | Description |
|---------|-------|-------------|
| `--list-profiles` | `--profiles` | Lists all available dart profiles |

### Backup & Restore 🆕

| Command | Description |
|---------|-------------|
| `--backup [name]` | Creates full backup (configuration, profiles, logs) |
| `--backup-config [name]` | Creates configuration-only backup |
| `--backup-list` | Lists all available backups |
| `--backup-restore <file>` | Restores backup from file |
| `--backup-cleanup [count]` | Deletes old backups (keeps the last N) |

### Testing Commands

| Command | Description |
|---------|-------------|
| `--test-updater` | Starts interactive updater test menu |
| `--test-full` | Runs complete updater test suite |
| `--test-version` | Tests version checking |
| `--test-retry` | Tests retry mechanism |
| `--test-logging` | Tests logging system |

### Runtime Options

| Command | Short | Description |
|---------|-------|-------------|
| `--verbose` | `-vv` | Enables verbose logging (starts GUI) |
| `--beta` | | Enables beta tester mode (starts GUI) |

## Detailed Descriptions

### Help and Information

```bash
# Shows complete help
darts-hub --help

# Shows version (automatically retrieved from Updater.cs)
darts-hub --version

# Shows detailed application information
darts-hub --info

# Shows system and environment information
darts-hub --system-info
```

**Example output for `--version`:**
```
DartsHub v1.2.0
Build Date: 2024-01-15 14:30:25
Runtime: .NET 6.0.36
Platform: Microsoft Windows NT 10.0.22631.0
Architecture: X64
```

### Backup & Restore Functionality 🆕

#### Full Backup
```bash
# Automatically named with timestamp
darts-hub --backup

# With custom name
darts-hub --backup my-important-backup
```

**Contents of a full backup:**
- Configuration files (`config.json`, `apps-downloadable.json`, `apps-local.json`, `apps-open.json`)
- Current log files (last 30 days)
- Wizard configuration
- Backup manifest with metadata

#### Configuration Backup
```bash
# Only save configuration
darts-hub --backup-config

# With custom name
darts-hub --backup-config my-config-backup
```

**Contents of a configuration backup:**
- `config.json`
- `apps-downloadable.json`
- `apps-local.json`
- `apps-open.json`
- Wizard configuration
- Backup manifest

#### Backup Management
```bash
# List all backups
darts-hub --backup-list

# Restore backup
darts-hub --backup-restore my-backup.zip
darts-hub --backup-restore backups/dartshub-backup-2024-01-15_14-30-25.zip

# Delete old backups (default: keep 10)
darts-hub --backup-cleanup
darts-hub --backup-cleanup 5  # Keep only the last 5
```

**Example output for `--backup-list`:**
```
=== AVAILABLE BACKUPS ===

1. dartshub-backup-2024-01-15_14-30-25
   Created: 2024-01-15 14:30:25
   Size: 2.45 MB
   Type: Full Backup
   Path: C:\path\to\backups\dartshub-backup-2024-01-15_14-30-25.zip

2. my-config-backup
   Created: 2024-01-15 12:15:10
   Size: 125.3 KB
   Type: Configuration Only
   Path: C:\path\to\backups\my-config-backup.zip
```

### Profile Management // NOT FINALLY IMPLEMENTED YET

```bash
# Lists all profiles with details
darts-hub --list-profiles
```

**Example output:**
```
=== DART PROFILES ===

Found 2 profile(s):

1. My Dart Setup
   Tagged for Start: Yes
   Applications: 3
     • darts-caller (Auto-start)
     • darts-wled (Auto-start)
     • darts-pixelit (Manual)

2. Test Configuration
   Tagged for Start: No
   Applications: 1
     • darts-caller (Manual)
```

### Testing Functions

#### Full Test
```bash
# Runs all updater tests
darts-hub --test-full
```

#### Interactive Test Menu
```bash
# Starts interactive menu for tests
darts-hub --test-updater
```

**Menu example:**
```
Available Tests:
1. Full Test Suite (all components)
2. Version Check Test
3. Retry Mechanism Test
4. Logging System Test
5. Exit

Select option (1-5):
```

#### Specific Tests
```bash
# Individual test types
darts-hub --test-version    # Only version checking
darts-hub --test-retry      # Only retry mechanism
darts-hub --test-logging    # Only logging system
```

### Runtime Options

#### Verbose Mode // NOT FINALLY IMPLEMENTED YET
```bash
# Starts GUI with verbose logging
darts-hub --verbose
```
- Enables detailed log outputs
- Sets environment variable `DARTSHUB_VERBOSE=true`
- Still starts the GUI

#### Beta Mode
```bash
# Starts GUI in beta tester mode
darts-hub --beta
```
- Enables beta release checking
- Enables experimental features
- Sets `Updater.IsBetaTester = true`

## Backup-Restore Workflow Examples

### Scenario 1: Regular Backup
```bash
# Weekly backup with date
darts-hub --backup weekly-backup-$(date +%Y-%m-%d)

# Clean up old backups (keep only last 4 weeks)
darts-hub --backup-cleanup 4
```

### Scenario 2: Before Important Changes
```bash
# Backup before changes
darts-hub --backup before-major-changes

# Make changes...
# If something goes wrong:
darts-hub --backup-restore before-major-changes.zip
```

### Scenario 3: Migration to New System
```bash
# On old system:
darts-hub --backup migration-backup

# Copy backup file to new system
# On new system:
darts-hub --backup-restore migration-backup.zip
```

### Scenario 4: Configuration Only Backup
```bash
# Quick configuration backup before experiments
darts-hub --backup-config quick-config-save

# Experiment with settings...
# If restoration needed:
darts-hub --backup-restore quick-config-save.zip
```

## Implementation Details

### CommandLineHelper Class

The `CommandLineHelper` class in `darts-hub/UI/CommandLineHelper.cs` manages all CLI functions and now automatically uses the version from `Updater.cs`:

```csharp
// Version is automatically retrieved from Updater.cs
Console.WriteLine($"Version: {Updater.version}");

// Main function for argument processing
public static async Task<bool> ProcessCommandLineArgs(string[] args)

// Backup functions
await BackupHelper.CreateFullBackup(customName);
await BackupHelper.CreateConfigBackup(customName);
BackupHelper.ListBackups();
await BackupHelper.RestoreBackup(backupFile);
BackupHelper.CleanupOldBackups(keepCount);
```

### BackupHelper Class

The new `BackupHelper` class in `darts-hub/UI/BackupHelper.cs` manages all backup operations:

```csharp
// Backup creation
public static async Task<string> CreateFullBackup(string customName = null)
public static async Task<string> CreateConfigBackup(string customName = null)

// Backup management
public static void ListBackups()
public static async Task<bool> RestoreBackup(string backupPath, bool interactive = true)
public static void CleanupOldBackups(int keepCount = 10)
```

### Integration in Program.cs

```csharp
[STAThread]
public static async Task<int> Main(string[] args) 
{
    // 1. Process CLI arguments
    bool shouldStartGui = await ShouldStartGui(args);
    
    if (!shouldStartGui)
        return 0; // Exit after CLI operation
    
    // 2. Start GUI normally
    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
```

### Backup File Structure

```
backup.zip
├── backup-manifest.txt           # Metadata and file list
├── config.json                  # Main configuration
├── apps-downloadable.json       # Downloadable apps configuration
├── apps-local.json              # Local apps configuration
├── apps-open.json               # Open apps configuration
├── control/
│   └── wizard/
│       └── WizardArgumentsConfig.json  # Wizard settings
├── profiles/                    # User profiles (only in Full Backup)
│   ├── profile1.json
│   └── profile2.json
└── logs/                       # Current logs (only in Full Backup)
    ├── 
    └── 
```

### Manifest Example

```
DartsHub Backup Manifest
========================

Backup Name: my-important-backup
Type: Full Backup
Created: 2024-01-15 14:30:25
DartsHub Version: v1.2.0
Platform: Microsoft Windows NT 10.0.22631.0

Files included:
  - config.json
  - apps-downloadable.json
  - apps-local.json
  - apps-open.json
  - control/wizard/WizardArgumentsConfig.json
  - profiles/profile1.json
  - profiles/profile2.json
  - logs/updater-2024-01-15.log

Total Files: 8
```

## Return Values

| Return Value | Meaning |
|--------------|---------|
| `0` | Successful execution |
| `1` | Execution error |

## Advanced Usage

### Combined Commands

```bash
# Verbose mode with other commands (starts GUI)
darts-hub --verbose --beta

# Test commands with parameters
darts-hub --test-updater full    # Direct full test
darts-hub --test-updater version # Direct version test
```

### Automation

```bash
# Batch testing with backup (Windows)
darts-hub --backup pre-test-backup
darts-hub --test-full > test-results.txt 2>&1
if %ERRORLEVEL% NEQ 0 darts-hub --backup-restore pre-test-backup.zip

# Shell scripting with backup (Linux/macOS)
#!/bin/bash
darts-hub --backup pre-test-backup
if darts-hub --test-full; then
    echo "Tests passed"
    darts-hub --backup-cleanup 5
else
    echo "Tests failed, restoring backup"
    darts-hub --backup-restore pre-test-backup.zip
fi
```

### CI/CD Integration

```yaml
# GitHub Actions example
- name: Backup Configuration
  run: ./darts-hub --backup-config ci-backup

- name: Run DartsHub Tests
  run: |
    ./darts-hub --test-full
    ./darts-hub --test-logging

- name: Restore on Failure
  if: failure()
  run: ./darts-hub --backup-restore ci-backup.zip
```


## Logging and Debugging

### Log Files
- All CLI operations are logged
- Backup operations receive detailed logs
- Location: `logs/` directory in application folder

### Verbose Output
```bash
# With --verbose flag
[14:30:25] Verbose mode enabled via command line
[14:30:26] Starting application initialization
[14:30:27] Loading configuration files
[14:30:28] Creating backup: my-backup...
[14:30:29] Backup completed: 2.45 MB
```

## Platform-Specific Notes

### Windows
```cmd
darts-hub.exe --help
darts-hub.exe --backup my-backup
darts-hub.exe --backup-list
```

### Linux/macOS
```bash
./darts-hub --help
./darts-hub --backup my-backup
./darts-hub --backup-list

# Or with chmod +x
chmod +x darts-hub
darts-hub --help
```

### PowerShell (Windows)
```powershell
.\darts-hub.exe --help
& ".\darts-hub.exe" --backup "my-backup"
& ".\darts-hub.exe" --backup-list
```

## Troubleshooting

### Common Issues

1. **"Command not found"**
   - Ensure the application path is correct
   - On Linux/macOS: Check execution permissions with `chmod +x`

2. **"Application is already running"**
   - A GUI instance is already running
   - Close the GUI or use Task Manager

3. **Backup errors**
   - Check write permissions in application directory
   - Ensure sufficient disk space is available

4. **Restore errors**
   - Check if backup file exists and is not corrupted
   - Ensure no application instance is running
