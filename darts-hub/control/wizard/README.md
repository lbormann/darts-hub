# Darts-Hub Setup Wizard System

## Overview

The Darts-Hub Setup Wizard is a modular, extensible system that guides users through the initial configuration of their dart applications. It provides a user-friendly interface for setting up complex configurations without requiring technical knowledge.

## Architecture

### Core Components

1. **SetupWizardManager** - Main controller for the wizard flow
2. **IWizardStep** - Interface that defines the structure of each wizard step
3. **WizardWindow** - The main wizard UI window with navigation controls
4. **Individual Wizard Steps** - Specific configuration steps for different applications

### Wizard Steps

1. **WelcomeWizardStep** - Introduction and overview of what will be configured
2. **CallerSetupWizardStep** - Configuration for darts-caller (core dart recognition)
3. **WledSetupWizardStep** - Configuration for WLED LED strip integration
4. **PixelitSetupWizardStep** - Configuration for Pixelit display integration  
5. **CompletionWizardStep** - Final summary and completion confirmation

## Features

### Automatic Launch
- The wizard automatically launches when the application starts for the first time
- It only shows if there are configurable applications available
- Can be disabled once completed

### Manual Launch
- Users can re-run the wizard at any time from the About section
- Useful for reconfiguring applications or when adding new apps

### Modular Design
- Easy to add new wizard steps for additional applications
- Each step is self-contained and handles its own validation
- Steps can be skipped if not applicable

### Smart Configuration
- Only shows steps for applications that are actually available in the profile
- Validates user input before proceeding to the next step
- Automatically enables applications that are configured

### User-Friendly Interface
- Progress bar showing current step and total steps
- Clear navigation with Next/Previous/Skip/Cancel options
- Visual indicators and helpful descriptions
- Error handling with user-friendly messages

## Adding New Wizard Steps

To add a new wizard step for a custom application:

1. **Create a new class implementing `IWizardStep`:**
```csharp
public class MyAppWizardStep : IWizardStep
{
    public string Title => "Configure My App";
    public string Description => "Set up My App integration";
    public string IconName => "myapp";
    public bool CanSkip => true;
    
    // Implement required methods...
}
```

2. **Add the step to SetupWizardManager.InitializeWizardSteps():**
```csharp
// Check if your app is available
var myApp = profile.Apps.Values.FirstOrDefault(a => 
    a.App.CustomName.ToLower().Contains("myapp"));
if (myApp != null)
{
    wizardSteps.Add(new MyAppWizardStep());
}
```

3. **Implement the required methods:**
- `Initialize()` - Set up references to profile and managers
- `CreateContent()` - Build the UI for your configuration step
- `ValidateStep()` - Validate user input before proceeding
- `ApplyConfiguration()` - Save the configuration to the application
- `OnStepShown()` - Load current values when step is displayed
- `OnStepHidden()` - Clean up when leaving the step
- `ResetStep()` - Reset form to default values

## Configuration Integration

The wizard integrates with the existing Darts-Hub configuration system:

- **Configurator** - Manages global application settings (wizard completed flag)
- **ProfileManager** - Handles application profiles and configurations  
- **AppBase.Configuration** - Individual application configurations
- **Arguments** - Specific configuration parameters for each application

## UI Design

The wizard follows Darts-Hub's dark theme with:
- Dark background colors (#FF2D2D30, #FF252526)
- Blue accent color for primary actions (#FF007ACC)
- Clear typography and spacing
- Consistent with the main application's design language

## Error Handling

The wizard includes comprehensive error handling:
- Validation errors are shown inline with helpful messages
- Connection errors are caught and displayed to the user
- Invalid configurations prevent progression to next steps
- Option to cancel wizard at any time without saving partial configurations

## Future Enhancements

Potential improvements for the wizard system:
- Save partial progress and allow resuming later
- Template-based step creation for common configuration patterns
- Integration with online documentation and help systems
- Automatic detection of devices on the network
- Import/export of wizard configurations

## Technical Notes

- Built using Avalonia UI framework
- Follows MVVM patterns where applicable
- Fully async/await pattern for non-blocking operations
- Comprehensive validation and error handling
- Modular architecture allows easy extension