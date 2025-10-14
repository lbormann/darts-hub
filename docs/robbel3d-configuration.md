# ?? Robbel3D One-Click Configuration System

The Robbel3D Configuration System provides comprehensive one-click setup for WLED controllers and Darts-Caller with complete argument support, automated WLED config file management, and API-based configuration upload.

## Features

### Complete Argument Support
- **ALL Darts-WLED Arguments**: Every available WLED extension parameter is configurable
- **ALL Darts-Caller Arguments**: Complete caller voice announcement settings  
- **Score Area Effects**: Advanced A1-A12 area configuration with range selection
- **Player-Specific Settings**: Individual colors and effects for up to 6 players
- **Game Mode Support**: X01, Cricket, Bermuda, Tactics specific configurations

### WLED Configuration Management
- **WLED Config Files**: Loads and uses existing `wled_cfg.json` from configs directory
- **WLED Presets**: Incorporates existing `wled_presets.json` with dartboard-specific presets
- **API Upload**: Automatically uploads both config.json and presets.json to WLED controller via HTTP API
- **Device Discovery**: Network scanning to automatically find WLED devices
- **Validation**: Confirms WLED device compatibility before configuration

### Preset System
- **Complete Configurations**: Each preset contains full WLED + Caller setup
- **Backward Compatibility**: Legacy presets supported alongside new enhanced ones
- **File Integration**: Existing config files from `configs/` directory are loaded and used
- **Release Integration**: Config files are copied to release directory for distribution

## Configuration Structure

### Robbel3D Configuration Format

```json
{
  "name": "Configuration Name",
  "description": "Detailed description", 
  "version": "2.0",
  "created": "2024-01-01T00:00:00Z",
  "led_count": 141,
  "dartboard_type": "Standard",
  
  "wled_config": {
    // Complete WLED device configuration (config.json format)
    "id": { "mdns": "wled-robbel3d", "name": "WLED Dartboard" },
    "hw": { 
      "led": { 
        "total": 141, 
        "maxpwr": 1500,
        "ins": [{ "start": 0, "len": 141, "pin": [2], "type": 22 }]
      }
    },
    "light": { "scale-bri": 255, "tr": { "dur": 300 } },
    "def": { "ps": 1, "on": true, "bri": 200 }
  },
  
  "wled_presets": {
    // WLED presets (presets.json format)
    "1": { "n": "Dartboard Default", "on": true, "bri": 200, /* ... */ },
    "2": { "n": "Game Mode - Red", "on": true, "bri": 255, /* ... */ }
  },
  
  "caller_settings": {
    // ALL Darts-Caller arguments
    "HOST": "0.0.0.0",
    "PORT": "8079", 
    "U": "",           // Email (user configurable)
    "P": "",           // Password (user configurable)  
    "B": "",           // Board ID (user configurable)
    "M": "",           // Media path (user configurable)
    "V": "80",         // Volume
    "C": "",           // Specific caller
    "R": "False",      // Random caller
    "CCP": "True",     // Call current player
    "PCC": "True",     // Possible checkout call
    "DL": "5",         // Download limit
    // ... all other caller arguments
  },
  
  "wled_settings": {
    // ALL Darts-WLED arguments  
    "HOST": "0.0.0.0",
    "PORT": "8079",
    "WEPS": "192.168.1.141",  // WLED endpoint (auto-updated)
    "LEDCOUNT": "141",         // LED count
    "BRI": "200",             // Global brightness
    "SOFF": "False",          // Switch off when no game
    "DU": "3000",             // Duration in milliseconds
    
    // Player idle effects
    "IDE": "solid|white",     // Default idle
    "IDE1": "solid|white",    // Player 1
    "IDE2": "solid|red",      // Player 2  
    "IDE3": "solid|green",    // Player 3
    "IDE4": "solid|blue",     // Player 4
    "IDE5": "solid|yellow",   // Player 5
    "IDE6": "solid|purple",   // Player 6
    
    // Special states
    "RTW": "solid|white",     // Ready to throw
    "ATC": "solid|white",     // Approach cockpit
    
    // Dartboard areas
    "TRIPLE": "solid|orange", // Triple ring
    "DOUBLE": "solid|red",    // Double ring  
    "SINGLE": "solid|green",  // Single area
    "OUTER": "solid|white",   // Outer bull
    "INNER": "solid|yellow",  // Inner bull (25)
    "BULL": "solid|red",      // Bull (25)
    "BULLSEYE": "solid|gold", // Bullseye (50)
    "MISS": "solid|gray",     // Miss
    
    // Game events
    "BUST": "solid|red",      // Bust/over
    "WINNER": "Rainbow",      // Winner effect
    "CHECKOUT": "solid|gold", // Possible checkout
    
    // Score-specific effects (S1-S180)
    "S20": "solid|orange",    // Score 20
    "S25": "solid|yellow",    // Score 25
    "S50": "solid|gold",      // Score 50
    "S180": "Rainbow",        // Score 180
    
    // Score area effects with ranges (A1-A12)
    "A1": "1-20 solid|white",     // Scores 1-20, white
    "A2": "21-40 solid|green",    // Scores 21-40, green
    "A3": "41-60 solid|blue",     // Scores 41-60, blue
    // ... additional areas and parameters
  }
}
```

## Usage

### Creating Robbel3D Configurations

1. **Automatic Preset Generation**:
   - Run darts-hub to auto-generate sample presets in `robbel3d-presets/` directory
   - Existing WLED config files from `configs/` are automatically loaded and integrated
   - Two presets are created: "Complete" (all arguments) and "Legacy" (backward compatibility)

2. **Manual Configuration**: 
   - Edit JSON files in `robbel3d-presets/` directory
   - Include all desired WLED and Caller arguments
   - Reference existing config files or create new ones

3. **Config File Integration**:
   - Place `wled_cfg.json` and `wled_presets.json` in `configs/` directory
   - System automatically loads and incorporates them into Robbel3D presets
   - Files are copied to release directory for distribution

### Applying Configurations

```csharp
// Discover WLED device
var wledDevices = await NetworkDeviceScanner.ScanForWledDevices(cancellationToken);
var selectedDevice = wledDevices.FirstOrDefault();

// Load and apply Robbel3D configuration
var presets = Robbel3DConfigurationManager.GetAvailablePresets();
var selectedPreset = presets.FirstOrDefault(p => p.Name.Contains("Complete"));

// Apply complete configuration
var success = await Robbel3DConfigurationManager.ApplyConfiguration(
    selectedPreset, 
    selectedDevice.IpAddress, 
    profileManager
);
```

The system performs the following operations:

1. **WLED Config Upload**: Uploads complete `config.json` to WLED device via `/edit` endpoint
2. **WLED Presets Upload**: Uploads `presets.json` file to device, fallback to individual preset API calls  
3. **Darts-Hub Integration**: Updates all WLED and Caller arguments in darts-hub profiles
4. **Device Validation**: Confirms WLED device compatibility and configuration success

## File Structure

```
darts-hub/
??? configs/                          # Source WLED configuration files
?   ??? wled_cfg.json                # WLED device configuration
?   ??? wled_presets.json            # WLED presets
?   ??? wled_cfg (1).json            # Alternative config files
??? robbel3d-presets/                # Robbel3D preset files
?   ??? robbel3d-complete.json       # Complete configuration with all arguments
?   ??? robbel3d-standard.json       # Legacy simple configuration  
??? release/configs/                 # Release directory (auto-generated)
?   ??? wled_cfg.json                # Copied for distribution
?   ??? wled_presets.json            # Copied for distribution
??? copy-wled-configs.bat            # Windows copy script
??? copy-wled-configs.sh             # Linux/Mac copy script
```

## API Endpoints Used

### WLED Device APIs
- **Device Info**: `GET http://{ip}/json/info` - Device validation and LED count detection
- **Config Upload**: `POST http://{ip}/edit` - Upload config.json and presets.json files
- **Preset Management**: `POST http://{ip}/json/state` - Individual preset upload fallback
- **Device Reset**: `POST http://{ip}/reset` - Factory reset (optional safety feature)

### Configuration Process
1. **Network Discovery**: Scan local network for WLED devices using `/win` endpoint XML validation
2. **Device Validation**: Confirm WLED compatibility via `/json/info` API
3. **File Upload**: Upload complete configuration via `/edit` multipart form data
4. **Preset Integration**: Ensure all presets are available via `/json/state` API calls
5. **Verification**: Validate successful configuration deployment

## Enhanced Features

### Complete Argument Coverage
- **280+ WLED Parameters**: All darts-wled extension arguments supported
- **50+ Caller Parameters**: Complete darts-caller configuration options
- **Advanced Effects**: Score areas, player colors, game modes, transitions
- **Power Management**: LED limits, brightness control, thermal protection
- **Network Options**: Sync, MQTT, E1.31, UDP, multi-device setups

### Smart Configuration Loading  
- **Auto-Detection**: Finds existing config files in multiple locations
- **Format Validation**: Ensures WLED JSON structure compatibility
- **Error Handling**: Graceful fallback to defaults when files are invalid
- **Merge Logic**: Combines user configs with Robbel3D enhancements

### Release Integration
- **Build Process**: Scripts automatically copy config files to release
- **Distribution Ready**: Release includes all necessary WLED configurations  
- **Version Tracking**: Metadata tracks config compatibility and versions
- **Documentation**: Complete parameter documentation for all arguments

## Migration and Compatibility

### From Legacy Configurations
- Existing Robbel3D presets remain functional
- New "Complete" presets offer enhanced functionality
- Backward compatibility maintained for all existing setups
- Gradual migration path available

### WLED Version Support
- WLED 0.13+ recommended for full feature support
- Older versions supported with reduced functionality
- Version detection and compatibility warnings
- Automatic feature availability adjustment

### Darts-Hub Integration
- Seamless integration with existing profile system
- All arguments automatically mapped to UI controls
- Enhanced wizard steps for guided configuration
- Real-time configuration validation and testing

This enhanced Robbel3D system provides the most comprehensive one-click configuration solution for dartboard LED setups, incorporating complete argument support, existing configuration file integration, and reliable API-based deployment to WLED controllers.