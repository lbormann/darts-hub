# DartsHub Config Manager - Linux/macOS Quick Start

## Installation

The `config-manager.sh` script is included in your DartsHub installation.

## First-Time Setup

Make the script executable:

```bash
cd /path/to/darts-hub
chmod +x config-manager.sh
```

## Usage

Simply run the script:

```bash
./config-manager.sh
```

## Main Menu

```
================================================
           DartsHub CLI Manager
        Configuration Export/Import Tool
================================================

What would you like to do?

=== Configuration Management ===
[1] Export Configuration
[2] Import Configuration
[3] List Available Extensions
[4] List Exports
[5] View Export Information

=== Backup & Restore ===
[6] Create Backup
[7] Restore Backup
[8] List Backups
[9] Cleanup Old Backups

=== Information ===
[10] Application Info
[11] System Info
[12] List Profiles
[13] Version Info

=== Testing ===
[14] Updater Tests

=== Other ===
[15] Help
[0]  Exit
```

## Features

### Color-Coded Interface
- **Green**: Normal text and success messages
- **Yellow**: Warnings
- **Red**: Errors and dangerous operations
- **Blue**: Information

### Auto-Detection
- Automatically checks for `darts-hub` executable
- Ensures proper execution permissions
- Clear error messages if something is missing

### Cross-Platform Compatibility
- Works on Linux (all distributions)
- Works on macOS (Intel and Apple Silicon)
- Same functionality as Windows version
- Compatible with bash and zsh shells

## Quick Examples

### Create a backup
```bash
./config-manager.sh
# Choose [6] Create Backup
# Choose [1] Full Backup
# Enter optional name or press Enter
```

### Export configuration
```bash
./config-manager.sh
# Choose [1] Export Configuration
# Choose [1] Full Configuration
# Enter optional name and description
```

### Import configuration
```bash
./config-manager.sh
# Choose [2] Import Configuration
# Choose [1] Select from available exports (merge mode)
# Select file number
```

## Troubleshooting

### Script won't run
```bash
# Make sure it's executable
chmod +x config-manager.sh

# Check if file exists
ls -la config-manager.sh
```

### "darts-hub executable not found"
```bash
# Make sure you're in the correct directory
pwd

# Should show path to DartsHub installation
# If not, navigate to it:
cd /path/to/darts-hub
```

### Permission denied
```bash
# Make both scripts executable
chmod +x config-manager.sh
chmod +x darts-hub
```

### Colors not showing
```bash
# Your terminal should support ANSI colors
# Most modern terminals do by default
# If not, the script will still work, just without colors
```

## Advanced Usage

### Run from anywhere
Add to your PATH or create an alias:

```bash
# In ~/.bashrc or ~/.zshrc
alias darts-config='cd /path/to/darts-hub && ./config-manager.sh'

# Then reload:
source ~/.bashrc  # or ~/.zshrc

# Now run from anywhere:
darts-config
```

### Automation
While the script is interactive, you can use direct CLI commands for automation:

```bash
# Direct commands bypass the menu
./darts-hub --export my-backup
./darts-hub --backup
./darts-hub --list-exports
```

## Keyboard Shortcuts

- **Enter**: Confirm selection or continue
- **Ctrl+C**: Exit script immediately
- **Any key**: Continue after viewing information

## Platform-Specific Notes

### Linux
- Works with any desktop environment
- Terminal emulator required (gnome-terminal, konsole, xterm, etc.)
- No additional dependencies needed

### macOS
- Works with Terminal.app
- Works with iTerm2 and other terminal emulators
- Compatible with both Intel and Apple Silicon Macs
- Supports both bash and zsh

## Getting Help

For more detailed documentation:
- Press `[15]` in the main menu for built-in help
- See `docs/config-export-import.md` for export/import guide
- See `docs/command-line-interface.md` for CLI reference
- Check `README.md` for general DartsHub information

## Support

For issues or questions:
- GitHub: https://github.com/lbormann/darts-hub
- Check logs in `logs/` directory
- Review error messages carefully
- Ensure all files have proper permissions

## Version History

- **v1.0** (2024) - Initial Linux/macOS release
  - Complete feature parity with Windows version
  - Color-coded interface
  - Automatic permission handling
  - All 15 menu options
  - Bash/Zsh compatibility
