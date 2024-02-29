# AUTODARTS-DESKTOP
[![Downloads](https://img.shields.io/github/downloads/lbormann/autodarts-desktop/total.svg)](https://github.com/lbormann/autodarts-desktop/releases/latest)

Autodarts-desktop (ADD) manages several apps for https://autodarts.io.
It automatically downloads and updates apps, provides configuration windows, and allows the launching of apps through a curated list of profiles.

If you are looking for an video explaination (german), have a look at https://www.youtube.com/watch?v=L5qx0afyXKs


List of Apps managed by ADD:

* [autodarts-client](https://github.com/autodarts/releases)
* [autodarts-caller](https://github.com/lbormann/autodarts-caller)
* [autodarts-extern](https://github.com/lbormann/autodarts-extern)
* [autodarts-wled](https://github.com/lbormann/autodarts-wled)
* [autodarts-gif](https://github.com/lbormann/autodarts-gif)
* [autodarts-voice](https://github.com/lbormann/autodarts-voice)
* [cam-loader](https://github.com/lbormann/cam-loader)
* [droid-cam](https://www.dev47apps.com)
* [epoc-cam](https://www.elgato.com/de/epoccam)
* [virtual-darts-zoom](https://lehmann-bo.de/?p=28)
* [dartboards-client](https://dartboards.online/client)
* custom-web-url-*
* custom-*

Disclaimer: Some apps could not be visible in your setup. The reason for that is that some apps are not available for your os.


## COMPATIBILITY

Autodarts-desktop supports all major platforms:

| OS | X64 | X86 | ARM | ARM64
| ------------- | ------------- | ------------- | ------------- | ------------- | 
| Windows | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: |
| Linux | :heavy_check_mark: |  | :heavy_check_mark: | :heavy_check_mark: |
| macOS | :heavy_check_mark: |  |  | :heavy_check_mark: |

Autodarts-desktop is built with Avalonia. check it out: https://docs.avaloniaui.net/


![alt text](https://github.com/lbormann/autodarts-desktop/blob/main/images/main.png?raw=true)
![alt text](https://github.com/lbormann/autodarts-desktop/blob/main/images/configuration.png?raw=true)



## HOW TO RUN

### Single-Command (Linux, MacOS)

You can install autodarts-desktop on a Linux system by using this single command.
It will automatically download the latest version and configures it for autostart.
You might have to install `curl` on your machine beforehand.
You can do so with `sudo apt install curl`.

```bash
bash <(curl -sL get-add.arnes-design.de)
```


### Step-by-step (Linux, macOS, Windows)

1) Create a new folder called "autodarts-desktop" in your home directory.
2) Download the appropiate zip-file. You can find it in the release section.
3) Extract the zip-file to the new folder "autodarts-desktop".
4) Linux and MacOS: Execute the following commands in a terminal
        
    ```bash
    chmod +x ~/autodarts-desktop/autodarts-desktop
    ```
    ```bash
    systemctl disable autodarts.service
    ```

5) MacOS: disable os-app-verification:

    ```bash
    sudo spctl --master-disable
    ```

    Unfortunately I couldn't find a proper way without doing this.
    Moreover make sure you do initial installation-steps for macos in case you want to use autodarts-client (https://docs.autodarts.io/getting-started/installation/)

6) Start the application by double-click autodarts-desktop 




## HOW-TO-REMOVE

### Single-Command (Linux, MacOS)

You can remove autodarts-desktop by passing `--uninstall` flag as follows.

```bash
bash <(curl -sL get-add.arnes-design.de) --uninstall
```



### Step-by-step (Linux, macOS, Windows)

1. Remove the folder called "autodarts-desktop" in your home directory.
2. Linux and MacOS: Execute the following commands in a terminal:

   ```bash
   systemctl enable autodarts.service
   ```

3. MacOS: enable os-app-verification:

   ```bash
   sudo spctl --master-enable
   ```


## USAGE

### Basics

Profiles representing different play-scenarios, whether you only want to use autodarts-caller or playing extern by using autodarts-extern.
Every app in a profile can be marked for start by checking it. Apps that are mandantory for particular profile can't be unchecked.
To start a selected profile click the button next to profile selection. On profile-start the application will check every included app for existence, installs it or updates it if it isn't up-to-date. If an app needs configuration, autodarts-desktop will display a configuration window to organize that. Configuration is explained in the next section.
To close a running app use the ecks-symbol. To see an apps output click the monitor-symbol that should appear after a short time since app-start. It shows full details of app events in realtime. 
You can also rename every app: use a right click for that; you can always return to the default name by entering an empty value.

### App-configuration

Some Apps have mandatory configuration fields to work properly. Those fields are highlighted by a red colored frame. You can also spot a mandatory field by asterik character (*) at the end of the particular field-name. To reset a field-value click on the rubber-symbol.
For an extensive App explaintion and its configuration click the question mark-symbol in the upper-right corner.
If your done filling out configuration fields just close the dialog window to save configuration. In case the app is still running, ADD will automatically restart it to apply current configuration. 

### Custom-apps

Imagine you could start your favorite apps along autodarts-*. That is what custom-apps are made for. As an example: You could start OBS, to stream an autodarts-game.. or trigger a Home-Assistant-hook to turn on your AD-Build.. or just some lights. 

### Specific App-version

If you would like to stay on specific app-version, create an empty file called "my_version.txt" in particular app dir. That file will stop future app-updates, until you remove the file.


## TROUBLESHOOTING

### App isn't starting at all

If you're running autodarts-desktop on windows the affected app is probably classified as a virus. The easiest way to verify this is to close ADD. After that, add the main folder (autodarts-desktop) as an exception for screening. Restart ADD and try to start the app.

## CONTRIBUTING

You just finished your new extension and wonder how to integrate it in ADD -> just send me a message on discord ;)



## RESOURCES

- Icon by <a href="https://freeicons.io/profile/8178">Ognjen Vukomanov</a> on <a href="https://freeicons.io">freeicons.io</a>
- Icon by <a href="https://freeicons.io/profile/823">Muhammad Haq</a> on <a href="https://freeicons.io">freeicons.io</a>                             
- Icon by <a href="https://freeicons.io/profile/85671">Mubdee Ashrafi</a> on <a href="https://freeicons.io">freeicons.io</a>    
- Icon by <a href="https://freeicons.io/profile/205927">Flatart</a> on <a href="https://freeicons.io">freeicons.io</a>
