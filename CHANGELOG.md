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
