# AUTODARTS-DESKTOP
[![Downloads](https://img.shields.io/github/downloads/lbormann/autodarts-desktop/total.svg)](https://github.com/lbormann/autodarts-desktop/releases/latest)

Autodarts-desktop manages several apps for https://autodarts.io.
It automatically manages downloads and updates, provides a configuration interface, and allows for the launching of applications through a curated list of profiles.


## COMPATIBILITY

Autodarts-desktop supports all major platforms:

| OS | X64 | X86 | ARM | ARM64
| ------------- | ------------- | ------------- | ------------- | ------------- | 
| Windows | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |
| Linux | :heavy_check_mark: |  | :heavy_check_mark: | :heavy_check_mark: |
| macOS | :heavy_check_mark: |  |  | :heavy_check_mark: |

Autodarts-desktop is built with Avalonia. check it out: https://docs.avaloniaui.net/


![alt text](https://github.com/lbormann/autodarts-desktop/blob/main/MAIN.PNG?raw=true)
![alt text](https://github.com/lbormann/autodarts-desktop/blob/main/SETTINGS.PNG?raw=true)



## INSTALL INSTRUCTION

### Windows - Linux - MacOS

1) Create a new folder in your user/home directory
2) Download the appropiate zip-file in the release section.
3) Extract the zip-file to the new folder
4) On Linux / MacOS you need to make it executable:

        cd /home/<new-folder>
        chmod +x autodarts-desktop

    MacOS: By the time you need to disable os-app-verification:

        sudo spctl --master-disable 

    Unfortunately I couldn't find a proper way without doing this.
    Moreover make sure you do initial installation-steps for macos in case you want to use autodarts-client (https://docs.autodarts.io/getting-started/installation/)

5) Open autodarts-desktop by double-click



### Usage

#### Basics

Profiles representing different play-scenarios, whether you only want to use autodarts-caller or playing extern by using autodarts-extern.
Every app in a profile can be marked for start by checking it. Apps that are mandantory for particular profile can't be unchecked.
To start a selected profile use the dart-symbol next to profile selection. On profile-start the application will check every included app for existence, installs it or updates it if it isn't up-to-date. If an app needs configuration, autodarts-desktop will display a configuration window to organize that. Configuration is explained in the next section.
To close a running app use the ecks-symbol. To see an apps output click the monitor-symbol that should appear after a short time since app-start. It shows full details of app events in realtime. 

#### App-configuration
Some Apps have mandatory configuration fields to work properly. Those fields are highlighted by a red colored frame. You can also spot a mandatory field by asterik character (*) at the end of the particular field-name. To reset a field-value click on the rubber-symbol.
For an extensive App explaintion and its configuration click the question mark-symbol in the upper-right corner.
If your done filling out configuration fields just close the dialog window to save configuration. In case your app is still running, you need to close it first to apply current configuration. 



## TODOs


### Done
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
- Add new app: droidcam (android) + epoccam (iOS)
- Recreate *.json files on error
- Fixes autodarts-caller bool-arguments
- Fixes arguments of type 'float'
- Mark required fields in app-settings
- Fixes rerun of apps fail
- Fixes highlighting of required arguments in settings
- Fix typo on argument required
- Try to start app after user filled required argument
- cross-platform
- update images / text in Readme
- update when autostart is activated
- Fix arguments-type-float without range!
- Improve README description
- Kill app-process on macOS
- Fix Updater on macOS


## Resources

Icon by <a href="https://freeicons.io/profile/8178">Ognjen Vukomanov</a> on <a href="https://freeicons.io">freeicons.io</a>
Icon by <a href="https://freeicons.io/profile/823">Muhammad Haq</a> on <a href="https://freeicons.io">freeicons.io</a>                             
Icon by <a href="https://freeicons.io/profile/85671">Mubdee Ashrafi</a> on <a href="https://freeicons.io">freeicons.io</a>                             
