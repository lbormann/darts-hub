# AUTODARTS-DESKTOP

Autodarts-desktop manages several extension-tools for https://autodarts.io.
It automatically handles download, setup, use updates to recent versions.
 - You can start tools like caller or extern to transfer thrown darts to other web-dart-platforms.
 - The app-administration is simplified by configuration windows - no more console/batch-file handling. =)

## Presentation
![alt text](https://github.com/Semtexmagix/autodarts-desktop/blob/master/Main.png?raw=true)


## Examples
autodarts-caller (Setup-page):
 - calls your thrown score/ possible checkouts accordingly to the state of an https://autodarts.io match.
 ![alt text](https://github.com/Semtexmagix/autodarts-desktop/blob/master/SetupCaller.png?raw=true)
  - for more info about autodarts-caller visit https://github.com/lbormann/autodarts-caller

Autodarts-extern (Setup-page):
 - automates multiple dart-web-platforms accordingly to the state of an https://autodarts.io match.
 ![alt text](https://github.com/Semtexmagix/autodarts-desktop/blob/master/SetupExtern.png?raw=true)
  - for more info about autodarts-extern visit https://github.com/lbormann/autodarts-extern

You can also use other assistant-programs:
 - dartboards-client (for webcam support with dartboards.online)
 - visual-darts-zoom (for an image zoom onto thrown darts)
 ![alt text](https://github.com/Semtexmagix/autodarts-desktop/blob/master/vdz.png?raw=true)
  - for more info about visual-darts-zoom visit https://lehmann-bo.de/
  - for more info about dartboards-client visit https://dartboards.online/


Moreover you can start custom-apps, e.g. OBS (Open Brodcast System) - to compose a customized virtual-camera:

![alt text](https://github.com/Semtexmagix/autodarts-desktop/blob/master/OBS4.PNG?raw=true)
Since autodarts-client version 0.18.0-rc1 it's possible to stream your board cams!


![alt text](https://github.com/Semtexmagix/autodarts-desktop/blob/master/OBS2.png?raw=true)
With OBS it is possible to display the autodarts-match in the virtual camera.


![alt text](https://github.com/Semtexmagix/autodarts-desktop/blob/master/OBS1.png?raw=true)
It is also possible, to integrate a player camera into the view.


![alt text](https://github.com/Semtexmagix/autodarts-desktop/blob/master/OBS3.png?raw=true)
or .. a full screen mode for the board.

This can be realized by OBS scenes: The scenes can be switched during the game or via hotkeys.

Hint: If you start OBS by "custom-app function", use "--startvirtualcam --disable-updater --minimize-to-tray" for start-arguments! OBS will start in optimal way. 


## BUGS

It may be buggy. You can give me feedback in Discord (Autodarts.io) ---> Reepa86


## TODOs
- cross-platform
- update images / text in Readme
- update when autostart is activated
- Do not put any folder oder files in autodarts-desktops`s extensions folders
- Update without question!

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


## LAST WORDS
Thanks to Timo for awesome https://autodarts.io.
Thanks to Wusaaa for the caller and the extern tools and also for the many help =D 
