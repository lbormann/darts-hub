# AUTODARTS-DESKTOP
[![Downloads](https://img.shields.io/github/downloads/lbormann/autodarts-desktop/total.svg)](https://github.com/lbormann/autodarts-desktop/releases/latest)

Autodarts-desktop manages several apps for https://autodarts.io.
It handles automatic download/updates, offers a configuration-gui and starting of apps by a list of curated profiles.

WIP: MacOS isn't fully working yet!


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

Download the appropiate file for your os in the release section.
On Linux / MacOS you probably need to make it executable (chmod +x autodarts-desktop)



## BUGS

It may be buggy. I've just coded it for fast fun with https://autodarts.io. You can give me feedback in Discord > wusaaa




## TODOs

- Improve README description
- Kill app-process on macOS
- Fix Updater on macOS


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


## Resources

Icon by <a href="https://freeicons.io/profile/8178">Ognjen Vukomanov</a> on <a href="https://freeicons.io">freeicons.io</a>
Icon by <a href="https://freeicons.io/profile/823">Muhammad Haq</a> on <a href="https://freeicons.io">freeicons.io</a>                             
Icon by <a href="https://freeicons.io/profile/85671">Mubdee Ashrafi</a> on <a href="https://freeicons.io">freeicons.io</a>                             
