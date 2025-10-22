## v1.2.4
### Improvements
    - added enable on startup checkbox in newsettingsmode
    - remove toolbar area for custom apps
### Robbel3D Mode
    - Stay Tuned for more Infos
### Darts-WLED
    - Stability and Performance improvements

## v1.2.3
### Bug Fixes
    - custom names for Links and apps whear leading into darts-hub crash. Fixed now.
    - removed cmd window on windows at startup

## v1.2.2
### New Features
    - Command Line Interface (CLI) Support
        - Complete CLI functionality for headless operations
        - Interactive and non-interactive command execution
        - Support for PowerShell, CMD, and Bash environments
    - Comprehensive Backup & Restore System
        - Full system backups (configuration, profiles, logs)
        - Configuration-only backups for quick saves
        - Interactive backup restoration with safety confirmations
        - Automatic backup cleanup with configurable retention
        - Detailed backup manifests with version tracking
    - Enhanced Testing Framework
        - Extended CLI testing commands for all updater components
        - Interactive test menus for guided testing
        - Comprehensive logging test functionality
        - Version checking and retry mechanism testing

### Bug Fixes
    - Fixed PowerShell CLI output issues by changing from WinExe to Exe output type
    - Resolved console allocation problems for CLI operations
    - Corrected backup restoration to include all configuration files
    - Fixed CLI argument processing and command recognition

## v1.2.0
### Improvements
    - Updater System Overhaul
        - New robust update checking and installation system
        - Retry mechanism with exponential backoff for network requests
    - Comprehensive logging system with daily log files
    - Improved error handling and user feedback during updates
    - Setup Wizard Enhancements
      - Improved user guidance and error handling in setup wizards

### Bug Fixes
    - fixed skip this step button in setup wizard
    - build routine bugfixes

## v1.1.10
- Setup Wizard System
  - Complete setup wizard implementation for all extensions
  - Step-by-step configuration guidance for new users
  - Automated extension detection and configuration
  - Network device scanning capabilities
  - Integrated completion steps with validation
- Improvements
    - Added viewport settings to setup wizard window for better scaling and adaptive content
    - Wizard window now properly scales and adjusts content when resized
- Bug Fixes
  - Fixed wizard navigation edge cases
  - Resolved configuration saving issues in specific scenarios
  - Corrected extension argument validation errors
  - Fixed memory leaks in wizard cleanup processes
- new ehnaced settings mode
  - test the new possibility to configure your settings especially for wled.


## b1.0.8
- Improvements
    - Added viewport settings to setup wizard window for better scaling and adaptive content
    - Wizard window now properly scales and adjusts content when resized

## b1.0.7
- Bug Fixes
    - short WLED start after connection test in wizzard

## b1.0.6
- Bug Fixes
  - Fixed wizard navigation edge cases
  - Resolved configuration saving issues in specific scenarios
  - Corrected extension argument validation errors
  - Fixed memory leaks in wizard cleanup processes

## b1.0.5
- Setup Wizard System
  - Complete setup wizard implementation for all extensions
  - Step-by-step configuration guidance for new users
  - Automated extension detection and configuration
  - Network device scanning capabilities
  - Integrated completion steps with validation

- Extension Setup Wizards
  - Caller setup wizard with essential settings, camera config, and detection steps
  - WLED setup wizard with board status, player colors, and effect configurations
  - GIF extension setup wizard with media configuration and display modes
  - Pixelit extension setup wizard with player and game animations
  - Generic extension wizard support for future extensions

- Enhanced User Experience
  - Improved argument control factories for all extensions
  - Better configuration validation and error handling
  - Streamlined extension selection process
  - Enhanced settings content provider with wizard integration

- Technical Improvements
  - Refactored wizard architecture with modular step system
  - Improved configuration management and validation
  - Enhanced network device discovery functionality
  - Better separation of concerns in wizard components

## b1.0.4
- Beta tester functionality
  - Add support for beta release checking and downloading
  - Users can opt-in to receive pre-release versions
  - Separate handling for stable and beta version checking
  - Enhanced version detection for pre-release versions

- WLED Extension
  - Updated WLED extension components
  - Improved WLED API integration
  - Enhanced color definitions and score area handling
  - Better WLED settings management

## b1.0.0
- Complete UI redesign
  - more user friendly
  - more modern
  - Tooltips for all arguments
  - viewport settings

## 0.13.19
- WLED
  - nothing new

- PIXELIT
  - nothing new

- Caller
  - reactivate possibility to use the Caller standalone via source
  - changed webcaller design
  - add new soundpacks
  - add new languages IT, RU
  - add more realistic caller behaviour arg -CRL "--caller-real-life" (just X01)
  - add if no playername soundkey avaialble play "player one, two....."

## 0.13.16
- WLED
- backend changes

- PIXELIT
- backend changes

- Caller
- implementet more detailes user Stats to debug faster
  - no personal data will be stored
- prepare for new soundpacks and languages
  - new variable voices
  - RU, IT will come in the future

- USER NEED TO UPDATE TO GET CALLER RUNNING IN THE FUTURE
- You need to Update to this version, otherwhise the caller will not run anymore after mid of Mai

## 0.13.15
- version Update for Darts-Caller

- IMPORTANT NOTE!!!!!!!
- You need to Update to this version, otherwhise the caller will not run anymore after mid of Mai

## 0.13.10
- Bugfix -Caller PCC argument was wrong typed float insted of int
- Bugfix -DU argument doesnt effect the player Idle effect anymore
- add Random Checkout to caller

## 0.13.9
- Caller - Bugfix PlayerIndex reset in Turnament mode

## 0.13.8
- Version Updates Caller and WLED
- WLED
  - improved ATC, RTW, Chricket, Bermuda, tactics support
  - add -SOFF argument to turn off the lights when WLED controller is connected!
  - IDE2 - IDE6 available now to set collors/presets for each player

  - add mac intel build

- Caller
  - add Tactics

  - add mac intel build

## 0.13.5
- Bugfix Extensions where not closed after closing Darts-Hub

## 0.13.3
- Major GUI chnages. 
  - collabsable Settings
  - new Design
  - Settings reordered

- Bugfix description Parsing

## 0.12.22
- Caller version update: Bermuda: if you dont hit the target Broadcast busted for extensions

## 0.12.21
- automatic paring of Readme.md files
  - use parsed values for tooltips
- Darts-Caller Update v2.17.7
  - Bermuda: if you dont hit the target, Broadcast Busted for extensions

## 0.12.18
- Major Caller Update in V2.17.6
  - Gamemodes
  - Soundpacks
  - Hostserver
- WLED Update v1.7.1
  -add Gamemodes
- added Tooltips for Darts-Caller configuration
- added Tooltips for Darts-WLED configuration
## 0.12.15
- Major Caller Update in V2.17.0

## 0.12.11
- Webcaller bugfix


## 0.12.10
- Checkbox removed
- IF YOU WANT TO BE A BETA TESTER YOU CAN ACTIVATE THE BETA TESTER CHECKBOX, 
  THE LATEST PRE RELEASE WILL BE DOWNLOADED AUTOMATICALLY. 
  PLEASE BE CAREFUL AND BACKUP YOUR darts-hub FOLDER COMPLETELY BEFORE YOU DO IT!!!


- update darts-pixel to version 1.2.2
- bugfix beta check

## 0.12.8
- update darts-pixel to version 1.2.2
- migration bugfix

## 0.12.6
- update darts-wled to version 1.7.0
    - div new fetures
- update darts-caller to version 2.16.1
- update darts-pixel to version 1.2.1

## 0.12.5
- update darts-wled to version 1.6.0
    - div new fetures
- update darts-caller to version 2.15.0

## 0.12.4
- update darts-wled to version 1.5.3


## 0.12.3
- update darts-wled to version 1.5.2

## 0.12.1

- adapt app versions


## 0.12.0

- rename application to darts-hub


## 0.11.2

- adapt app versions


## 0.11.1

- fix typo


## 0.11.0

- adapt app versions
- add changelog for apps
- close app on profile uncheck


## 0.10.41

- adapt app versions
- closing all apps on start


## 0.10.40

- adapt app versions


## 0.10.39

- adapt app versions


## 0.10.38

- adapt app versions
- fix callers argument resets


## 0.10.37

- adapt app versions
- fix monitor clean-up


## 0.10.36

- adapt app versions


## 0.10.35

- adapt app versions
- clear monitor logs


## 0.10.34

- adapt app versions


## 0.10.33

- adapt app versions


## 0.10.32

- add extension autodarts-pixelit
- adapt app versions


## 0.10.31

- adapt app versions


## 0.10.30

- adapt app versions


## 0.10.29

- adapt app versions


## 0.10.28

- adapt app versions


## 0.10.27

- adapt app versions


## 0.10.26

- adapt app versions


## 0.10.25

- adapt app versions


## 0.10.24

- adapt app versions


## 0.10.23

- adapt app versions


## 0.10.22

- adapt app versions


## 0.10.21

- adapt app versions


## 0.10.20

- hotfix: save configuration when needed


## 0.10.19

- hotfix: close apps + save configuration when needed


## 0.10.18

- fix possible config destroying on system shutdown
- adapt app versions


## 0.10.17

- adapt app versions


## 0.10.16

- add possibility to exclude an app for updates by placing an (empty) file with name "my_version" in respective app dir


## 0.10.15

- adapt app versions


## 0.10.14

- adapt app versions


## 0.10.13

- adapt app versions


## 0.10.12

- adapt app versions


## 0.10.11

- adapt app versions


## 0.10.10

- adapt app versions


## 0.10.9

- add button to view changelog
- adapt app versions


## 0.10.8

- adapt app versions


## 0.10.7

- open url-based apps only once
- add changelog on version-check
- add expiring messages
- adapt app versions


## 0.10.6

- fix behaviour for offline-/timeout scenarios
- run apps, ordered by custom-name


## 0.10.5

- improve app-descriptions
- fix crash on exit config-window


## 0.10.4

- fix permission-error on custom-app start
- fix start of local-app for unconfigured argument-string
- run app after an app-update
- add popup description for apps


## 0.10.3

- adapt app versions


## 0.10.2

- adapt app versions


## 0.10.1

- adapt app versions


## 0.10.0

- adapt app versions
- restart an app automatically when configuration has changed


## 0.9.23

- add CHANGELOG
- add BACKLOG
- adapt app versions
- add app-renaming-feature
- add more custom-apps options
- adjust all app-arg-names matching app ones (only avail on new version / no-migration)


## 0.0.0

- refactor setup-areas for using AppManager
- stop starting custom app multiple times
- close custom-app on exit
- fully reworked project; use custom language to manage apps and profiles and create gui dynamically
- add reinstall-option as of download can fail (e.g. this app is not available on your os)
- do not update installed apps after new release when apps are the same size
- Check at start if there are any profiles, else close app with msg
- Arguments: required field depends on other field
- prevent argument-serialization if attribute isRuntimeArgument == true
- mark required config fields on open dialog
- start installable apps after download
- find app`s executable on storage
- run as admin
- add new app: droidcam (android) + epoccam (iOS)
- recreate *.json files on error
- fixes autodarts-caller bool-arguments
- fixes arguments of type 'float'
- mark required fields in app-settings
- fixes rerun of apps fail
- fixes highlighting of required arguments in settings
- fix typo on argument required
- try to start app after user filled required argument
- cross-platform
- update images / text in Readme
- update when autostart is activated
- fix arguments-type-float without range!
- improve README description
- kill app-process on macOS
- fix Updater on macOS
