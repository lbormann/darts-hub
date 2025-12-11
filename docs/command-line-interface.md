# DartsHub Command Line Interface (CLI)

DartsHub supports various command line commands for testing, debugging, backup/restore, configuration export/import, and system management.

## Overview

```bash
# General usage
dartshub [COMMAND] [OPTIONS]

# Examples
dartshub --help
dartshub --version
dartshub --test-full
dartshub --backup my-backup
dartshub --export my-config
dartshub --import my-config.json
dartshub --list-profiles
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

### Configuration Export/Import 🆕

| Command | Alias | Description |
|---------|-------|-------------|
| `--export [name]` | `--export-full` | Exports complete configuration (all extensions) |
| `--export-ext <names...>` | `--export-extension` | Exports specific extension(s) |
| `--export-params` | `--export-parameters` | Exports specific parameters (interactive) |
| `--import <file> [mode]` | | Imports configuration (mode: merge/replace) |
| `--list-extensions` | `--extensions` | Lists all available extensions |
| `--list-params <ext>` | `--params` | Lists parameters for an extension |
| `--list-exports` | `--exports` | Lists all export files |
| `--export-info <file>` | `--info-export` | Shows information about an export file |

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
dartshub --help

# Shows version (automatically retrieved from Updater.cs)
dartshub --version

# Shows detailed application information
dartshub --info

# Shows system and environment information
dartshub --system-info
```

**Example output for `--version`:**
```
DartsHub v1.2.0
Build Date: 2024-01-15 14:30:25
Runtime: .NET 6.0.36
Platform: Microsoft Windows NT 10.0.22631.0
Architecture: X64
```

### Configuration Export/Import 🆕

The configuration export/import system allows you to backup, share, and restore extension configurations. See [Configuration Export/Import Guide](config-export-import.md) for detailed documentation.

#### Export Full Configuration

```bash
# Export all extensions with automatic name
darts-hub --export

# Export with custom name
darts-hub --export my-config

# Export with name and description
darts-hub --export my-config "Backup before v2.0 update"
```

#### Export Specific Extensions

```bash
# Export single extension
darts-hub --export-ext darts-caller

# Export multiple extensions
darts-hub --export-ext darts-caller darts-wled darts-pixelit
```

**Example output:**
```
=== CONFIG EXPORT - EXTENSIONS ===

Exporting 2 extension(s)...

✓ Export successful!
   Export file: C:\Users\...\darts-hub\exports\export_extensions_darts-caller-darts-wled_20240115_143022.json

   Exported extensions:
      • darts-caller
      • darts-wled
```

#### Export Specific Parameters (Interactive)

```bash
darts-hub --export-params
```

> **Note for Windows users**: Interactive commands (those requiring keyboard input) will open in a new console window. This is necessary to properly capture your input. The window will remain open after the command completes - press Enter to close it.

This starts an interactive session where you can:
1. Select which extensions to export from
2. For each extension, select which parameters to export

**Example session:**
```
=== CONFIG EXPORT - PARAMETERS ===

This command exports specific parameters from specific extensions.

Available extensions:
  1. darts-caller
  2. darts-wled
  3. darts-pixelit

Select extensions (comma-separated numbers, or 'all'): 1,2

Selected 2 extension(s).

--- darts-caller ---
  Available parameters:
    1. U
    2. P
    3. B
    4. M
    5. C
    ...

  Select parameters (comma-separated numbers, 'all', or 'skip'): 1,2,3,4

  ✓ Selected 4 parameter(s)

--- darts-wled ---
  ...
```

#### Import Configuration

```bash
# Import with merge mode (default) - safe, updates existing
darts-hub --import my-config.json

# Import with replace mode - replaces everything
darts-hub --import my-config.json replace

# Short filename works too
darts-hub --import export_full_20240115_143022.json
```

> **Note for Windows users**: The import command requires confirmation and will open in a new console window for input.

**Example output:**
```
=== CONFIG IMPORT ===

Export file: my-config.json
Export type: Extensions
Created: 2024-01-15 14:30:22
Extensions: darts-caller, darts-wled
Description: Backup before v2.0 update

Import mode: Merge

Proceed with import? (y/N): y

Importing configuration...
(A backup will be created automatically)

✓ Import successful!

   Merged configuration: 0 added, 2 updated
   Backup created: backups/config-backups/apps-config_import_20240115_144530.json
```

#### List Available Extensions

```bash
darts-hub --list-extensions
```

**Output:**
```
  Found 5 extension(s):
     • darts-caller
     • darts-wled
     • darts-pixelit
     • darts-gif
     • darts-voice
```

#### List Extension Parameters

```bash
darts-hub --list-params darts-caller
```

**Output:**
```
=== PARAMETERS FOR: darts-caller ===

Found 45 parameter(s):
   • U
   • P
   • B
   • M
   • MS
   • V
   • C
   • R
   ...
```

#### List Export Files

```bash
darts-hub --list-exports
```

**Output:**
```
  Found 3 export file(s):

     • export_full_20240115_143022.json
       Created: 2024-01-15 14:30:22
       Size: 125.45 KB

     • export_extensions_darts-caller_20240114_093015.json
       Created: 2024-01-14 09:30:15
       Size: 15.23 KB

     • export_parameters_20240113_161045.json
       Created: 2024-01-13 16:10:45
       Size: 8.67 KB
```

#### Show Export Information

```bash
darts-hub --export-info my-config.json
