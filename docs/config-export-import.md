# Configuration Export/Import System

This document describes the configuration export/import system for DartsHub, which allows you to backup, share, and restore extension configurations from the `apps-downloadable.json` file.

## Overview

The export/import system provides three types of exports:

1. **Full Export**: Exports all extensions with all their configurations
2. **Extension Export**: Exports specific extensions completely
3. **Parameter Export**: Exports only specific parameters from specific extensions

All exports are stored as JSON files with metadata that describes the content, making it easy to understand what's included before importing.

## Platform Notes

### Windows PowerShell / CMD

**Important**: In PowerShell and CMD, you must use `.\darts-hub.exe` or the full path to run the program, not just `darts-hub`.

Examples:
```powershell
# PowerShell
.\darts-hub.exe --help
.\darts-hub.exe --export my-config
.\darts-hub.exe --list-extensions

# CMD
darts-hub.exe --help
darts-hub.exe --export my-config
```

**Interactive commands** (`--export-params`, `--import`) will open in a **new console window** to properly capture keyboard input. This is a Windows limitation when running from PowerShell or CMD. The console window will remain open after the command completes - press Enter to close it.

### Linux / macOS
All commands work directly in your terminal without opening new windows. If darts-hub is in your PATH, you can run:

```bash
darts-hub --help
darts-hub --export my-config
```

## Features

- ? Automatic backup before any export or import operation
- ? Metadata tracking (type, timestamp, app version, description)
- ? Multiple import modes (merge or replace)
- ? Interactive parameter selection
- ? Non-destructive operations (original file is never modified without backup)
- ? CLI and programmatic API
- ? Human-readable export files

## CLI Commands

### Export Commands

#### Export Full Configuration

```bash
# Windows PowerShell
.\darts-hub.exe --export
.\darts-hub.exe --export my-config
.\darts-hub.exe --export my-config "Backup before v2.0 update"

# Linux / macOS
darts-hub --export
darts-hub --export my-config
darts-hub --export my-config "Backup before v2.0 update"
```

#### Export Specific Extensions

```bash
# Windows PowerShell
.\darts-hub.exe --export-ext darts-caller
.\darts-hub.exe --export-ext darts-caller darts-wled darts-pixelit

# Linux / macOS
darts-hub --export-ext darts-caller
darts-hub --export-ext darts-caller darts-wled darts-pixelit
```

#### Export Specific Parameters

```bash
# Windows PowerShell
.\darts-hub.exe --export-params

# Linux / macOS
darts-hub --export-params
```

This will guide you through:
1. Selecting extensions
2. For each extension, selecting which parameters to export

### Import Commands

#### Import Configuration

```bash
# Import with merge mode (default) - adds new, updates existing
darts-hub --import exports/my-config.json

# Import with replace mode - replaces entire configuration
darts-hub --import exports/my-config.json replace

# Short filename works too
darts-hub --import my-config.json
```

Import modes:
- **Merge** (default): Updates existing entries and adds new ones. Non-destructive.
- **Replace**: Completely replaces the current configuration with the imported one.

### Information Commands

#### List Available Extensions

```bash
darts-hub --list-extensions
darts-hub --extensions
```

#### List Extension Parameters

```bash
darts-hub --list-params darts-caller
darts-hub --params darts-caller
```

#### List Export Files

```bash
darts-hub --list-exports
darts-hub --exports
```

#### Show Export File Information

```bash
# Show metadata and contents of an export file
darts-hub --export-info exports/my-config.json
darts-hub --export-info my-config.json
```

## Export File Structure

Export files are JSON files with metadata that describes the content.

### All Export Types (NEW Unified Format) ??

**All export types** (Full, Extensions, Parameters) now use the same optimized format:
- Only **NameHuman** and **Value** are exported for each parameter
- Parameters without values are automatically excluded
- Compact, efficient format that's easy to share

```json
{
  "type": "full",
  "version": "1.0",
  "timestamp": "2024-01-15T10:30:00",
  "appVersion": "1.0.0",
  "description": "Full configuration export (NameHuman + Value only)",
  "extensionNames": ["darts-caller", "darts-wled", "darts-pixelit"],
  "parameterData": {
    "darts-caller": [
      {
        "name": "M",
        "nameHuman": "-M / --media_path",
        "value": "C:\\temp\\caller"
      },
      {
        "name": "MS",
        "nameHuman": "-MS / --media_soundpack",
        "value": "google_en-GB-Neural2-A"
      }
    ],
    "darts-wled": [
      {
        "name": "WEPS",
        "nameHuman": "-WEPS / --wled_ip",
        "value": "192.168.1.100"
      },
      {
        "name": "IDE",
        "nameHuman": "-IDE / --idle_preset",
        "value": "1"
      }
    ]
  },
  "data": []
}
```

### Legacy Format (Old Exports)

Old exports used the `data` field with full extension configurations. These are still supported for import (backwards compatibility), but new exports use the optimized `parameterData` format.

### Metadata Fields

- `type`: Type of export (`full`, `extensions`, or `parameters`)
- `version`: Export format version (currently "1.0")
- `timestamp`: When the export was created
- `appVersion`: darts-hub version that created the export
- `description`: Optional description
- `extensionNames`: List of extension names included
- `parameterNames`: (For parameter exports) List of parameter names per extension
- `parameterData`: (For parameter exports) Simplified parameter data with only NameHuman and Value
- `data`: The actual configuration data (empty for parameter exports)

### Parameter Export/Import Behavior

**Export (All Types):** ??
- ? **ALL export types** now use the optimized format (Full, Extensions, Parameters)
- ? Only parameters with **non-empty values** are exported
- ? Only **NameHuman** and **Value** fields are included (not the entire parameter definition)
- ? Drastically reduces file size compared to old format
- ? Focuses on actual configuration values, not metadata
- ? Makes exports human-readable and easy to review

**Import (All Types):**
- ? Parameters are matched by their internal **Name** (e.g., "U", "P", "WEPS")
- ? Only parameters with **different values** are updated
- ? If the Value in the import matches the current Value, it's skipped
- ? Works in both **Merge** and **Replace** modes
- ? Safe: Won't overwrite if values are the same
- ? **Backwards compatible**: Old exports (with `data` field) still work

**Benefits:**
- ?? **Smaller file sizes**: No unnecessary metadata
- ?? **Easy to review**: Just names and values
- ?? **Faster operations**: Less data to process
- ?? **More secure**: Less information to leak
- ? **Consistent**: All export types work the same way


## Use Cases

### Scenario 1: Backup Before Update

```bash
# Windows PowerShell
.\darts-hub.exe --export pre-update-backup "Backup before v2.0 update"

# Linux / macOS
darts-hub --export pre-update-backup "Backup before v2.0 update"
```

### Scenario 2: Share Caller Configuration

```bash
# Windows PowerShell
.\darts-hub.exe --export-ext darts-caller

# The system will ask:
# "Do you want to EXCLUDE these credentials from export? (y/N):"
# Answer 'y' to safely share with friends!

# Linux / macOS
darts-hub --export-ext darts-caller
```

**Important**: When sharing, always exclude credentials (answer 'y') so your Autodarts account stays secure!

### Scenario 3: Save Specific Settings

```bash
# Windows PowerShell (opens new console window)
.\darts-hub.exe --export-params

# Linux / macOS
darts-hub --export-params

# Then select:
# 1. Extension: darts-wled
# 2. Parameters: WEPS, IDE, G, M, B
```

### Scenario 4: Restore After Problem

```bash
# List available exports
.\darts-hub.exe --list-exports  # Windows PowerShell
darts-hub --list-exports        # Linux / macOS

# Check what's in the export
.\darts-hub.exe --export-info my-backup.json  # Windows PowerShell
darts-hub --export-info my-backup.json        # Linux / macOS

# Restore with merge (safe)
.\darts-hub.exe --import my-backup.json  # Windows PowerShell
darts-hub --import my-backup.json        # Linux / macOS

# Or restore with replace (full restore)
.\darts-hub.exe --import my-backup.json replace  # Windows PowerShell
darts-hub --import my-backup.json replace        # Linux / macOS
```

### Scenario 5: Move Settings to New PC

1. On old PC:
```bash
# Windows PowerShell
.\darts-hub.exe --export complete-setup "All my settings for new PC"

# Linux / macOS
darts-hub --export complete-setup "All my settings for new PC"
```

2. Copy `exports/complete-setup_*.json` to new PC

3. On new PC:
```bash
# Windows PowerShell
.\darts-hub.exe --import complete-setup_*.json

# Linux / macOS
darts-hub --import complete-setup_*.json
```

## Automatic Backups

Every export and import operation automatically creates a backup of the current `apps-downloadable.json` file. Backups are stored in:

```
backups/config-backups/
  apps-config_export_full_20240115_103045.json
  apps-config_export_extensions_20240115_110230.json
  apps-config_import_20240115_123456.json
```

The backup filename indicates:
- Operation type (export/import)
- Export type (full/extensions/parameters)
- Timestamp

## Programmatic API

You can also use the export/import system programmatically:

```csharp
using darts_hub.control;

// Export full configuration
var exportPath = await ConfigExportManager.ExportFull("my-config", "Optional description");

// Export specific extensions
var extensions = new List<string> { "darts-caller", "darts-wled" };
var exportPath = await ConfigExportManager.ExportExtensions(extensions);

// Export specific parameters
var parameters = new Dictionary<string, List<string>>
{
    { "darts-caller", new List<string> { "U", "P", "B" } },
    { "darts-wled", new List<string> { "WEPS", "IDE" } }
};
var exportPath = await ConfigExportManager.ExportParameters(parameters);

// Import with merge
var result = await ConfigExportManager.Import("exports/my-config.json", ImportMode.Merge);

// Import with replace
var result = await ConfigExportManager.Import("exports/my-config.json", ImportMode.Replace);

// Get export information without importing
var info = await ConfigExportManager.GetExportInfo("exports/my-config.json");
Console.WriteLine($"Type: {info.Type}");
Console.WriteLine($"Extensions: {string.Join(", ", info.ExtensionNames)}");

// List available extensions
var extensions = await ConfigExportManager.ListAvailableExtensions();

// List parameters for an extension
var params = await ConfigExportManager.ListExtensionParameters("darts-caller");
```

## Best Practices

1. **Create backups regularly**: Use `--export` to create full backups before major changes
2. **Use descriptive names**: Add meaningful names and descriptions to your exports
3. **Test imports in merge mode first**: Always try merge mode before replace mode
4. **Check export info before importing**: Use `--export-info` to see what will be imported
5. **Keep important exports**: Don't rely only on automatic backups; manually save important configurations
6. **Document your changes**: Use the description field to explain what changed
7. **Protect your credentials** ??:
   - Always answer 'y' when exporting for sharing
   - Only include credentials in personal backups
   - Delete credential-containing exports after use
   - Never commit exports with credentials to public repositories

## Troubleshooting

### Import fails with "Extension not found"

This happens when importing parameters for an extension that doesn't exist in your current configuration. The import will skip that extension and warn you.

**Solution**: First import or add the extension, then import its parameters.

### Export file not found

If you use just a filename (not a full path), the system looks in the `exports/` directory.

**Solution**: Either use the full path or just the filename without path.

### Backup directory full

The system doesn't automatically clean up config backups (unlike full backups).

**Solution**: Manually clean the `backups/config-backups/` directory, keeping only recent backups you need.

## File Locations

- **Exports**: `exports/`
- **Config Backups**: `backups/config-backups/`
- **Source Config**: `apps-downloadable.json`

## Version Compatibility

Export files include the darts-hub version that created them. This helps identify potential compatibility issues. The system will attempt to import exports from any version, but you may encounter issues with significantly older or newer versions.

## Security Notes

?? **WARNING**: Export files contain your configuration, which may include:
- Autodarts credentials (email, password, board ID)
- API keys
- Local file paths
- Other sensitive information

### Automatic Credentials Protection ??

When exporting configurations that include the **darts-caller** extension, the system will automatically prompt you:

```
? WARNING: Your configuration includes 'darts-caller'
   This extension contains sensitive Autodarts credentials:
   - Email (U)
   - Password (P)
   - Board ID (B)

Do you want to EXCLUDE these credentials from export? (y/N):
```

**If you answer 'y' (yes)**:
- Parameters U, P, and B will be excluded from the export
- Safe to share the export file with others
- You can still export all other darts-caller settings

**If you answer 'n' (no)**:
- All parameters including credentials will be exported
- Keep this file private and secure
- Useful for personal backups only

This protection applies to:
- ? Full Configuration Export (`--export`)
- ? Extension Export (`--export-ext darts-caller`)
- ? Parameter Export (`--export-params`) - credentials excluded from selection

**Recommendations**:
- **For sharing**: Answer 'y' to exclude credentials
- **For personal backups**: Answer 'n' to include everything
- **For moving to new PC**: Answer 'n', then delete export after importing
- Don't share exports publicly if they contain credentials
- Remove sensitive values before sharing
- Use separate exports for sharing (parameter exports) vs backups (full exports)
- Store backups securely

## Future Enhancements

Potential future features:
- Encryption for sensitive exports
- Cloud backup integration
- Scheduled automatic exports
- Export templates
- Configuration comparison/diff
- Batch import/export
- Web interface for export management
