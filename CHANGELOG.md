## 0.12.10
- Checkbox removed
- IF YOU WANT TO BE A BETA TESTER YOU CAN ACTIVATE THE BETA TESTER CHECKBOX, 
  THE LATEST PRE RELEASE WILL BE DOWNLOADED AUTOMATICALLY. 
  PLEASE BE CAREFUL AND BACKUP YOUR darts-hub FOLDER COMPLETELY BEFORE YOU DO IT!!!!


- update darts-pixel to version 1.2.2
- bugfix beta check

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
