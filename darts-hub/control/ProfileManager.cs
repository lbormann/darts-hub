using Avalonia.Styling;
using ColorTextBlock.Avalonia;
using darts_hub.model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using File = System.IO.File;
using Path = System.IO.Path;


namespace darts_hub.control
{

    /// <summary>
    /// Manages everything around apps-lifecycle.
    /// </summary>
    public class ProfileManager
    {

        // ATTRIBUTES

        private string? dartboardsClientDownloadUrl;
        private string? droidCamDownloadUrl;
        private string? epocCamDownloadUrl;
        private string? dartsCallerDownloadUrl;
        private string? dartsExternDownloadUrl;
        private string? dartsWledDownloadUrl;
        private string? dartsPixelitDownloadUrl;
        private string? dartsGifDownloadUrl;
        private string? dartsVoiceDownloadUrl;
        private string? camLoaderDownloadUrl;
        private string? virtualDartsZoomDownloadUrl;
        


        private readonly string appsDownloadableFile = "apps-downloadable.json";
        private readonly string appsInstallableFile = "apps-installable.json";
        private readonly string appsLocalFile = "apps-local.json";
        private readonly string appsOpenFile = "apps-open.json";
        private readonly string profilesFile = "profiles.json";


        public event EventHandler<AppEventArgs>? AppDownloadStarted;
        public event EventHandler<AppEventArgs>? AppDownloadFinished;
        public event EventHandler<AppEventArgs>? AppDownloadFailed;
        public event EventHandler<DownloadProgressChangedEventArgs>? AppDownloadProgressed;

        public event EventHandler<AppEventArgs>? AppInstallStarted;
        public event EventHandler<AppEventArgs>? AppInstallFinished;
        public event EventHandler<AppEventArgs>? AppInstallFailed;
        public event EventHandler<AppEventArgs>? AppConfigurationRequired;

        private List<AppBase> AppsAll;
        public List<AppDownloadable> AppsDownloadable { get; private set; }
        private List<AppInstallable> AppsInstallable;
        private List<AppLocal> AppsLocal;
        private List<AppOpen> AppsOpen;
        private List<Profile> Profiles;





        // METHODS

        public ProfileManager()
        {
            var basePath = Helper.GetAppBasePath();
            appsDownloadableFile = Path.Combine(basePath, appsDownloadableFile);
            appsInstallableFile = Path.Combine(basePath, appsInstallableFile);
            appsLocalFile = Path.Combine(basePath, appsLocalFile);
            appsOpenFile = Path.Combine(basePath, appsOpenFile);
            profilesFile = Path.Combine(basePath, profilesFile);
            CreateDummyDownloadMaps();
        }



        public void LoadAppsAndProfiles()
        {
            AppsAll = new();
            AppsDownloadable = new();
            AppsInstallable = new();
            AppsLocal = new();
            AppsOpen = new();

            Profiles = new();

            if (File.Exists(appsDownloadableFile))
            {
                try
                {
                    var appsDownloadable = JsonConvert.DeserializeObject<List<AppDownloadable>>(File.ReadAllText(appsDownloadableFile));
                    AppsDownloadable.AddRange(appsDownloadable);
                    MigrateAppsDownloadable();
                    AppsAll.AddRange(AppsDownloadable);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException(appsDownloadableFile, ex.Message);
                }
            }
            else
            {
                CreateDummyAppsDownloadable();
            }

            if (File.Exists(appsInstallableFile))
            {
                try
                {
                    var appsInstallable = JsonConvert.DeserializeObject<List<AppInstallable>>(File.ReadAllText(appsInstallableFile));
                    AppsInstallable.AddRange(appsInstallable);
                    MigrateAppsInstallable();
                    AppsAll.AddRange(AppsInstallable);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException(appsInstallableFile, ex.Message);
                }
            }
            else
            {
                CreateDummyAppsInstallable();
            }


            if (File.Exists(appsLocalFile))
            {
                try
                {
                    var appsLocal = JsonConvert.DeserializeObject<List<AppLocal>>(File.ReadAllText(appsLocalFile));
                    AppsLocal.AddRange(appsLocal);
                    MigrateAppsLocal();
                    AppsAll.AddRange(AppsLocal);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException(appsLocalFile, ex.Message);
                }
            }
            else
            {
                CreateDummyAppsLocal();
            }

            if (File.Exists(appsOpenFile))
            {
                try
                {
                    var appsOpen = JsonConvert.DeserializeObject<List<AppOpen>>(File.ReadAllText(appsOpenFile));
                    AppsOpen.AddRange(appsOpen);
                    MigrateAppsOpen();
                    AppsAll.AddRange(AppsOpen);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException(appsOpenFile, ex.Message);
                }
            }
            else
            {
                CreateDummyAppsOpen();
            }

            if (File.Exists(profilesFile))
            {
                try
                {
                    Profiles = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(profilesFile));
                    MigrateProfiles();
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException(profilesFile, ex.Message);
                }
            }
            else
            {
                CreateDummyProfiles();
            }





            // Sets apps`s custom-name to app-name if custom-name is empty
            foreach (var a in AppsAll)
            {
                if (String.IsNullOrEmpty(a.CustomName))
                {
                    a.CustomName = a.Name;
                }
            }
            
            
            foreach (var profile in Profiles)
            {
                foreach(KeyValuePair<string, ProfileState> profileLink in profile.Apps)
                {
                    var appFound = false;
                    foreach(var app in AppsAll)
                    {
                        if(app.Name == profileLink.Key)
                        {
                            appFound = true;
                            profileLink.Value.SetApp(app);
                            break;
                        }
                    }
                    if (!appFound) throw new Exception($"Profile-App '{profileLink.Key}' not found");
                }
            }


            foreach (var appDownloadable in AppsDownloadable)
            {
                appDownloadable.DownloadStarted += AppDownloadable_DownloadStarted;
                appDownloadable.DownloadFinished += AppDownloadable_DownloadFinished;
                appDownloadable.DownloadFailed += AppDownloadable_DownloadFailed;
                appDownloadable.DownloadProgressed += AppDownloadable_DownloadProgressed;
                appDownloadable.AppConfigurationRequired += App_AppConfigurationRequired;
            }
            foreach (var appInstallable in AppsInstallable)
            {
                appInstallable.DownloadStarted += AppDownloadable_DownloadStarted;
                appInstallable.DownloadFinished += AppDownloadable_DownloadFinished;
                appInstallable.DownloadFailed += AppDownloadable_DownloadFailed;
                appInstallable.DownloadProgressed += AppDownloadable_DownloadProgressed;
                appInstallable.InstallStarted += AppInstallable_InstallStarted;
                appInstallable.InstallFinished += AppInstallable_InstallFinished;
                appInstallable.InstallFailed += AppInstallable_InstallFailed;
                appInstallable.AppConfigurationRequired += App_AppConfigurationRequired;
            }
            foreach (var appLocal in AppsLocal)
            {
                appLocal.AppConfigurationRequired += App_AppConfigurationRequired;
            }
            foreach (var appOpen in AppsOpen)
            {
                appOpen.AppConfigurationRequired += App_AppConfigurationRequired;
            }
        }

        public void StoreApps()
        {
            SerializeApps(AppsDownloadable, appsDownloadableFile);
            SerializeApps(AppsInstallable, appsInstallableFile);
            SerializeApps(AppsLocal, appsLocalFile);
            SerializeApps(AppsOpen, appsOpenFile);
            SerializeProfiles(Profiles, profilesFile);
        }

        public void DeleteConfigurationFile(string configurationFile)
        {
            File.Delete(configurationFile);
        }

        public static bool RunProfile(Profile? profile)
        {
            if (profile == null) return false;

            var allAppsRunning = true;
            var appsTaggedForStart = profile.Apps.Where(x => x.Value.TaggedForStart).OrderBy(x => x.Value.App.CustomName);
            foreach (KeyValuePair<string, ProfileState> app in appsTaggedForStart)
            {
                // as here is no catch, apps-run stops when there is an error
                if (!app.Value.App.Run(app.Value.RuntimeArguments)) allAppsRunning = false;
            }
            return allAppsRunning;
        }

        public void CloseApps()
        {
            foreach (var app in AppsAll)
            {
                try
                {
                    app.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Closing failed for app: {app.Name} - {ex.Message}");
                }
            }
        }

        public List<Profile> GetProfiles()
        {
            return Profiles;
        }



        private void CreateDummyAppsLocal()
        {
            List<AppLocal> apps = new();

            AppLocal custom1 =
               new(
                   name: "custom-1",
                   descriptionShort: "Starts a program on your file-system"
                   );

            AppLocal custom2 =
                new(
                    name: "custom-2",
                    descriptionShort: "Starts a program on your file-system"
                    );

            AppLocal custom3 =
                   new(
                       name: "custom-3",
                       descriptionShort: "Starts a program on your file-system"
                       );

            AppLocal custom4 =
                   new(
                       name: "custom-4",
                       descriptionShort: "Starts a program on your file-system"
                       );

            AppLocal custom5 =
                   new(
                       name: "custom-5",
                       descriptionShort: "Starts a program on your file-system"
                       );


            apps.Add(custom1);
            apps.Add(custom2);
            apps.Add(custom3);
            apps.Add(custom4);
            apps.Add(custom5);

            AppsLocal.AddRange(apps);
            AppsAll.AddRange(apps);
            SerializeApps(apps, appsLocalFile);
        }

        private void MigrateAppsLocal()
        {
            // Add more migs..
        }


        private void CreateDummyAppsOpen()
        {
            List<AppOpen> apps = new();

            AppOpen customUrl1 =
                new(
                    name: "custom-url-1",
                    descriptionShort: "Opens a file or url"
                    );

            AppOpen customUrl2 =
                new(
                    name: "custom-url-2",
                    descriptionShort: "Opens a file or url"
                    );

            AppOpen customUrl3 =
                new(
                    name: "custom-url-3",
                    descriptionShort: "Opens a file or url"
                    );

            AppOpen customUrl4 =
                new(
                    name: "custom-url-4",
                    descriptionShort: "Opens a file or url"
                    );

            AppOpen customUrl5 =
                new(
                    name: "custom-url-5",
                    descriptionShort: "Opens a file or url"
                    );

            apps.Add(customUrl1);
            apps.Add(customUrl2);
            apps.Add(customUrl3);
            apps.Add(customUrl4);
            apps.Add(customUrl5);

            AppsOpen.AddRange(apps);
            AppsAll.AddRange(apps);
            SerializeApps(apps, appsOpenFile);
        }

        private void MigrateAppsOpen()
        {
            // Add more migs..
        }


        private void CreateDummyAppsInstallable()
        {
            List <AppInstallable> apps = new();

            if (dartboardsClientDownloadUrl != null)
            {
                AppInstallable dartboardsClient =
                new(
                    downloadUrl: dartboardsClientDownloadUrl,
                    name: "dartboards-client",
                    helpUrl: "https://dartboards.online/client",
                    descriptionShort: "Connects webcam to dartboards.online",
                    executable: "dartboardsonlineclient.exe",
                    defaultPathExecutable: Path.Join(Helper.GetUserDirectoryPath(), @"AppData\Local\Programs\dartboardsonlineclient"),
                    startsAfterInstallation: true
                    );
                apps.Add(dartboardsClient);
            }

            if (droidCamDownloadUrl != null)
            {
                AppInstallable droidCam =
                new(
                    downloadUrl: droidCamDownloadUrl,
                    name: "droid-cam",
                    helpUrl: "https://www.dev47apps.com",
                    descriptionShort: "Connects to your android phone- or tablet-camera",
                    defaultPathExecutable: @"C:\Program Files (x86)\DroidCam",
                    executable: "DroidCamApp.exe",
                    runAsAdminInstall: true,
                    startsAfterInstallation: false
                    );
                apps.Add(droidCam);
            }

            if (epocCamDownloadUrl != null)
            {
                AppInstallable epocCam =
                new(
                    downloadUrl: epocCamDownloadUrl,
                    name: "epoc-cam",
                    helpUrl: "https://www.elgato.com/de/epoccam",
                    descriptionShort: "Connects to your iOS phone- or tablet-camera",
                    defaultPathExecutable: @"C:\Program Files (x86)\Elgato\EpocCam",
                    // epoccamtray.exe
                    executable: "EpocCamService.exe",
                    runAsAdminInstall: false,
                    startsAfterInstallation: false,
                    isService: true
                    );
                apps.Add(epocCam);
            }

            AppsInstallable.AddRange(apps);
            AppsAll.AddRange(apps);
            SerializeApps(apps, appsInstallableFile);
        }

        private void MigrateAppsInstallable()
        {
            // Add more migs..
        }


        private void CreateDummyDownloadMaps()
        {

            // DEFINE DOWNLOAD-MAPS FOR APPS BY CURRENT OS 



            // DOWNLOADABLE

            var dartsCallerDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://github.com/lbormann/darts-caller/releases/download/***VERSION***/darts-caller.exe",
                LinuxX64 = "https://github.com/lbormann/darts-caller/releases/download/***VERSION***/darts-caller",
                LinuxArm64 = "https://github.com/lbormann/darts-caller/releases/download/***VERSION***/darts-caller-arm64",
                //LinuxArm = "https://github.com/lbormann/darts-caller/releases/download/***VERSION***/darts-caller-arm",
                MacX64 = "https://github.com/lbormann/darts-caller/releases/download/***VERSION***/darts-caller-macx64",
                MacArm64 = "https://github.com/lbormann/darts-caller/releases/download/***VERSION***/darts-caller-mac"
            };
            dartsCallerDownloadUrl = dartsCallerDownloadMap.GetDownloadUrlByOs("b2.19.6");


            var dartsExternDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://github.com/lbormann/darts-extern/releases/download/v***VERSION***/darts-extern.exe",
                LinuxX64 = "https://github.com/lbormann/darts-extern/releases/download/v***VERSION***/darts-extern",
                //LinuxArm64 = "https://github.com/lbormann/darts-extern/releases/download/v***VERSION***/darts-extern-arm64",
                //LinuxArm = "https://github.com/lbormann/darts-extern/releases/download/v***VERSION***/darts-extern-arm",
                MacX64 = "https://github.com/lbormann/darts-extern/releases/download/v***VERSION***/darts-extern-mac",
                MacArm64 = "https://github.com/lbormann/darts-extern/releases/download/v***VERSION***/darts-extern-mac"
            };
            dartsExternDownloadUrl = dartsExternDownloadMap.GetDownloadUrlByOs("1.6.0");


            var dartsWledDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://github.com/lbormann/darts-wled/releases/download/***VERSION***/darts-wled.exe",
                LinuxX64 = "https://github.com/lbormann/darts-wled/releases/download/***VERSION***/darts-wled",
                LinuxArm64 = "https://github.com/lbormann/darts-wled/releases/download/***VERSION***/darts-wled-arm64",
                //LinuxArm = "https://github.com/lbormann/darts-wled/releases/download/***VERSION***/darts-wled-arm",
                MacX64 = "https://github.com/lbormann/darts-wled/releases/download/***VERSION***/darts-wled-mac64",
                MacArm64 = "https://github.com/lbormann/darts-wled/releases/download/***VERSION***/darts-wled-mac"
            };
            dartsWledDownloadUrl = dartsWledDownloadMap.GetDownloadUrlByOs("b1.9.5");


            var dartsPixelitDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://github.com/lbormann/darts-pixelit/releases/download/v***VERSION***/darts-pixelit.exe",
                LinuxX64 = "https://github.com/lbormann/darts-pixelit/releases/download/v***VERSION***/darts-pixelit",
                LinuxArm64 = "https://github.com/lbormann/darts-pixelit/releases/download/v***VERSION***/darts-pixelit-arm64",
                //LinuxArm = "https://github.com/lbormann/darts-pixelit/releases/download/v***VERSION***/darts-pixelit-arm",
                MacX64 = "https://github.com/lbormann/darts-pixelit/releases/download/v***VERSION***/darts-pixelit-mac",
                MacArm64 = "https://github.com/lbormann/darts-pixelit/releases/download/v***VERSION***/darts-pixelit-mac"
            };
            dartsPixelitDownloadUrl = dartsPixelitDownloadMap.GetDownloadUrlByOs("1.2.3");


            var dartsGifDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://github.com/lbormann/darts-gif/releases/download/v***VERSION***/darts-gif.exe",
                LinuxX64 = "https://github.com/lbormann/darts-gif/releases/download/v***VERSION***/darts-gif",
                LinuxArm64 = "https://github.com/lbormann/darts-gif/releases/download/v***VERSION***/darts-gif-arm64",
                //LinuxArm = "https://github.com/lbormann/darts-gif/releases/download/v***VERSION***/darts-gif-arm",
                MacX64 = "https://github.com/lbormann/darts-gif/releases/download/v***VERSION***/darts-gif-mac64",
                MacArm64 = "https://github.com/lbormann/darts-gif/releases/download/v***VERSION***/darts-gif-mac"
            };
            dartsGifDownloadUrl = dartsGifDownloadMap.GetDownloadUrlByOs("1.1.0");


            var dartsVoiceDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://github.com/lbormann/darts-voice/releases/download/v***VERSION***/darts-voice.exe",
                LinuxX64 = "https://github.com/lbormann/darts-voice/releases/download/v***VERSION***/darts-voice",
                LinuxArm64 = "https://github.com/lbormann/darts-voice/releases/download/v***VERSION***/darts-voice-arm64",
                //LinuxArm = "https://github.com/lbormann/darts-voice/releases/download/v***VERSION***/darts-voice-arm",
                MacX64 = "https://github.com/lbormann/darts-voice/releases/download/v***VERSION***/darts-voice-mac",
                MacArm64 = "https://github.com/lbormann/darts-voice/releases/download/v***VERSION***/darts-voice-mac"
            };
            dartsVoiceDownloadUrl = dartsVoiceDownloadMap.GetDownloadUrlByOs("1.1.0");


            var camLoaderDownloadMap = new DownloadMap
            {
                WindowsX86 = "https://github.com/lbormann/cam-loader/releases/download/v***VERSION***/cam-loader.zip",
                WindowsX64 = "https://github.com/lbormann/cam-loader/releases/download/v***VERSION***/cam-loader.zip"
            };
            camLoaderDownloadUrl = camLoaderDownloadMap.GetDownloadUrlByOs("1.0.0");


            var virtualDartsZoomDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://www.lehmann-bo.de/Downloads/VDZ/Virtual Darts Zoom.zip"
            };
            virtualDartsZoomDownloadUrl = virtualDartsZoomDownloadMap.GetDownloadUrlByOs();




            // INSTALLABLE

            var dartboardsClientDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://dartboards.online/dboclient_***VERSION***.exe"
                //MacX64 = "https://dartboards.online/dboclient_***VERSION***.dmg"
            };
            dartboardsClientDownloadUrl = dartboardsClientDownloadMap.GetDownloadUrlByOs("0.9.2");


            var droidCamDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://github.com/dev47apps/windows-releases/releases/download/win-***VERSION***/DroidCam.Setup.***VERSION***.exe"
            };
            droidCamDownloadUrl = droidCamDownloadMap.GetDownloadUrlByOs("6.5.2");


            var epocCamDownloadMap = new DownloadMap
            {
                WindowsX64 = "https://edge.elgato.com/egc/windows/epoccam/EpocCam_Installer64_***VERSION***.exe"
                //MacX64 = "https://edge.elgato.com/egc/macos/epoccam/EpocCam_Installer_***VERSION***.pkg"
            };
            epocCamDownloadUrl = epocCamDownloadMap.GetDownloadUrlByOs("3_4_0");




        }

        private async void CreateDummyAppsDownloadable()
        {
            //var readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-caller/refs/heads/master/README.md"; // URL zur README-Datei
            //var parser = new ReadmeParser();
            //var argumentDescriptions = await parser.GetArgumentsFromReadme(readmeUrl);

            List<AppDownloadable> apps = new();

            if (!string.IsNullOrEmpty(dartsCallerDownloadUrl))
            {
                
                AppDownloadable dartsCaller =
                    new(
                        downloadUrl: dartsCallerDownloadUrl,
                        changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-caller/master/CHANGELOG.md",
                        name: "darts-caller",
                        helpUrl: "https://github.com/lbormann/darts-caller",
                        descriptionShort: "Calls out thrown points",
                        configuration: new(
                            prefix: "-",
                            delimitter: " ",
                            arguments: new List<Argument> {
                            new(name: "U", type: "string", required: true, nameHuman: "-U / --autodarts_email", section: "Autodarts"),
                            new(name: "P", type: "password", required: true, nameHuman: "-P / --autodarts_password", section: "Autodarts"),
                            new(name: "B", type: "string", required: true, nameHuman: "-B / --autodarts_board_id", section: "Autodarts"),
                            new(name: "M", type: "path", required: true, nameHuman: "-M / --media_path", section: "Media"),
                            new(name: "MS", type: "path", required: false, nameHuman: "-MS / --media_path_shared", section: "Media"),
                            new(name: "V", type: "float[0.0..1.0]", required: false, nameHuman: "-V / --caller_volume", section: "Media"),
                            new(name: "C", type: "string", required: false, nameHuman: "-C / --caller", section: "Calls"),
                            new(name: "R", type: "int[0..2]", required: false, nameHuman: "-R / --random_caller", section: "Random"),
                            new(name: "RL", type: "int[0..6]", required: false, nameHuman: "-RL / --random_caller_language", section: "Random"),
                            new(name: "RG", type: "int[0..2]", required: false, nameHuman: "-RG / --random_caller_gender", section: "Random"),
                            new(name: "CCP", type: "int[0..2]", required: false, nameHuman: "-CCP / --call_current_player", section: "Calls"),
                            new(name: "CBA", type: "bool", required: false, nameHuman: "-CBA / --call_bot_actions", section: "Calls", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"}),
                            new(name: "E", type: "int[0..3]", required: false, nameHuman: "-E / --call_every_dart", section: "Calls"),
                            new(name: "ETS", type: "bool", required: false, nameHuman: "-ETS / --call_every_dart_total_score", section: "Calls", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }),
                            new(name: "PCC", type: "int", required: false, nameHuman: "-PCC / --possible_checkout_call", section: "Calls"),
                            new(name: "PCCYO", type: "bool", required: false, nameHuman: "-PCCYO / --possible_checkout_call_yourself_only", section: "Calls", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"}),
                            new(name: "A", type: "float[0.0..1.0]", required: false, nameHuman: "-A / --ambient_sounds", section: "Calls"),
                            new(name: "AAC", type: "bool", required: false, nameHuman: "-AAC / --ambient_sounds_after_calls", section: "Calls", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"}),
                            new(name: "DL", type: "int[0..100]", required: false, nameHuman: "-DL / --downloads", section: "Downloads"),
                            new(name: "DLLA", type: "int[0..6]", required: false, nameHuman: "-DLLA / --downloads_language", section: "Downloads"),
                            new(name: "DLN", type: "string", required: false, nameHuman: "-DLN / --downloads_name", section: "Downloads"),
                            new(name: "ROVP", type: "bool", required: false, nameHuman: "-ROVP / --remove_old_voice_packs", section: "Downloads", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"}),
                            new(name: "BAV", type: "float[0.0..1.0]", required: false, nameHuman: "-BAV / --background_audio_volume", section: "Calls"),
                            new(name: "LPB", type: "bool", required: false, nameHuman: "-LPB / --local_playback", section: "Calls", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"}),
                            new(name: "WEBDH", type: "bool", required: false, nameHuman: "-WEBDH / --web_caller_disable_https", section: "Service", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"} ),
                            new(name: "HP", type: "int", required: false, nameHuman: "-HP / --host_port", section: "Service" ),
                            new(name: "DEB", type: "bool", required: false, nameHuman: "-DEB / --debug", section: "Service", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"} ),
                            new(name: "CC", type: "bool", required: false, nameHuman: "-CC / --cert_check", section: "Service", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"}),
                            new(name: "CRL", type: "bool", required: false, nameHuman: "-CRL / --caller_real_life", section: "Calls", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"}),
                            new(name: "CBS", type: "bool", required: false, nameHuman: "-CBS / --call_blind_support", section: "Calls", valueMapping: new Dictionary<string, string>{["True"] = "1",["False"] = "0"})
                            })
                        );
                
                apps.Add(dartsCaller);
                //foreach (var argument in dartsCaller.Configuration.Arguments)
                //{
                //    if (argumentDescriptions.TryGetValue(argument.Name, out var description))
                //    {
                //        argument.Description = description;
                //    }
                //}

            }

            if (!string.IsNullOrEmpty(dartsExternDownloadUrl)) {
                AppDownloadable dartsExtern =
                new(
                    downloadUrl: dartsExternDownloadUrl,
                    changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-extern/master/CHANGELOG.md",
                    name: "darts-extern",
                    helpUrl: "https://github.com/lbormann/darts-extern",
                    descriptionShort: "Bridges and automates other dart-platforms",
                    configuration: new(
                        prefix: "--",
                        delimitter: " ",
                        arguments: new List<Argument> {
                            new(name: "connection", type: "string", required: false, nameHuman: "--connection", section: "Service"),
                            new(name: "browser_path", type: "file", required: true, nameHuman: "--browser_path", section: "", description: "Path to browser. fav. Chrome"),
                            new(name: "autodarts_user", type: "string", required: true, nameHuman: "--autodarts_user", section: "Autodarts"),
                            new(name: "autodarts_password", type: "password", required: true, nameHuman: "--autodarts_password", section: "Autodarts"),
                            new(name: "autodarts_board_id", type: "string", required: true, nameHuman: "--autodarts_board_id", section: "Autodarts"),
                            new(name: "extern_platform", type: "selection[lidarts,nakka,dartboards]", required: true, nameHuman: "", isRuntimeArgument: true),
                            new(name: "time_before_exit", type: "int[0..150000]", required: false, nameHuman: "--time_before_exit", section: "Match"),
                            new(name: "lidarts_user", type: "string", required: false, nameHuman: "--lidarts_user", section: "Lidarts", requiredOnArgument: "extern_platform=lidarts"),
                            new(name: "lidarts_password", type: "password", required: false, nameHuman: "--lidarts_password", section: "Lidarts", requiredOnArgument: "extern_platform=lidarts"),
                            new(name: "lidarts_skip_dart_modals", type: "bool", required: false, nameHuman: "--lidarts_skip_dart_modals", section: "Lidarts"),
                            new(name: "lidarts_chat_message_start", type: "string", required: false, nameHuman: "--lidarts_chat_message_start", section: "Lidarts", value: "Hi, GD! Automated darts-scoring - powered by autodarts.io - Enter the community: https://discord.gg/bY5JYKbmvM"),
                            new(name: "lidarts_chat_message_end", type: "string", required: false, nameHuman: "--lidarts_chat_message_end", section: "Lidarts", value: "Thanks GG, WP!"),
                            new(name: "lidarts_cam_fullscreen", type: "bool", required: false, nameHuman: "--lidarts_cam_fullscreen", section: "Lidarts"),
                            new(name: "nakka_skip_dart_modals", type: "bool", required: false, nameHuman: "--nakka_skip_dart_modal", section: "Nakka"),
                            new(name: "dartboards_user", type: "string", required: false, nameHuman: "--dartboards_user", section: "Dartboards", requiredOnArgument: "extern_platform=dartboards"),
                            new(name: "dartboards_password", type: "password", required: false, nameHuman: "--dartboards_password", section: "Dartboards", requiredOnArgument: "extern_platform=dartboards"),
                            new(name: "dartboards_skip_dart_modals", type: "bool", required: false, nameHuman: "--dartboards_skip_dart_modals", section: "Dartboards"),
                        })
                );
                apps.Add(dartsExtern);
            }

            //readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-wled/refs/heads/main/README.md";
            //argumentDescriptions = await parser.GetArgumentsFromReadme(readmeUrl);
            if (!string.IsNullOrEmpty(dartsWledDownloadUrl))
            {
                
                var dartsWledArguments = new List<Argument> {
                        new(name: "CON", type: "string", required: false, nameHuman: "-CON / --connection", section: "Service"),
                        new(name: "WEPS", type: "string", required: true, isMulti: true, nameHuman: "-WEPS / --wled_endpoints", section: "Service"),
                        new(name: "DU", type: "int[0..1000]", required: false, nameHuman: "-DU / --effect_duration", section: "Board Stop Start"),
                        new(name: "BSS", type: "float[0.0..10.0]", required: false, nameHuman: "-BSS / --board_stop_start", section: "Board Stop Start"),
                        new(name: "BRI", type: "int[1..255]", required: false, nameHuman: "-BRI / --effect_brightness", section: "Service"),
                        new(name: "HFO", type: "int[2..170]", required: false, nameHuman: "-HFO / --high_finish_on", section: "Effects"),
                        new(name: "HF", type: "string", required: false, isMulti: true, nameHuman: "-HF / --high_finish_effects", section: "Effects"),
                        new(name: "IDE", type: "string", required: false, nameHuman: "-IDE / --idle_effect", section: "Service"),
                        new(name: "IDE2", type: "string", required: false, nameHuman: "-IDE2 / --idle_effect_player2", section: "Service"),
                        new(name: "IDE3", type: "string", required: false, nameHuman: "-IDE3 / --idle_effect_player3", section: "Service"),
                        new(name: "IDE4", type: "string", required: false, nameHuman: "-IDE4 / --idle_effect_player4", section: "Service"),
                        new(name: "IDE5", type: "string", required: false, nameHuman: "-IDE5 / --idle_effect_player5", section: "Service"),
                        new(name: "IDE6", type: "string", required: false, nameHuman: "-IDE6 / --idle_effect_player6", section: "Service"),
                        new(name: "G", type: "string", required: false, isMulti: true, nameHuman: "-G / --game_won_effects", section: "Effects"),
                        new(name: "M", type: "string", required: false, isMulti : true, nameHuman: "-M / --match_won_effects", section: "Effects"),
                        new(name: "B", type: "string", required: false, isMulti : true, nameHuman: "-B / --busted_effects", section: "Effects"),
                        new(name: "PJ", type: "string", required: false, isMulti : true, nameHuman: "-PJ / --player_joined_effects", section: "Lobby Effects"),
                        new(name: "PL", type: "string", required: false, isMulti : true, nameHuman: "-PL / --player_left_effects", section: "Lobby Effects"),
                        new(name: "DEB", type: "bool", required: false, nameHuman: "-DEB / --debug", section: "Service", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }),
                        new(name: "BSW", type: "bool", required: false, nameHuman: "-BSW / --board_stop_after_win", section: "Board Stop Start", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }),
                        new(name: "OFF", type: "bool", required: false, nameHuman: "-OFF / --wled_off", section: "Service", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }),
                        new(name: "SOFF", type: "bool", required: false, nameHuman: "-SOFF / --wled_off_at_start", section: "Service", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }),
                        new(name: "BSE", type: "string", required: false, isMulti: true, nameHuman: "-BSE / --board_stop_effect", section: "Board Status Effects"),
                        new(name: "TOE", type: "string", required: false, isMulti: true, nameHuman: "-TOE / --takeout_effect", section: "Board Status Effects"),
                        new(name: "CE", type: "string", required: false, isMulti: true, nameHuman: "-CE / --calibration_effect", section: "Board Status Effects"),
                        new(name: "DSBULL", type: "string", required: false, isMulti: true, nameHuman: "-DSBULL / --dart_score_BULL_effects", section: "Single Dart Effects !! still in Progress !!"),
                      //  new(name: "TEST", type: "string", required: false, isMulti: true, nameHuman: "test", section: "WLED")



                    };
                for (int i = 0; i <= 180; i++)
                {
                    var score = i.ToString();
                    Argument scoreArgument = new(name: "S" + score, type: "string", required: false, isMulti: true, nameHuman: "-S" + score + " / --score_" + score + "_effects", section: "Score Effects");
                    dartsWledArguments.Add(scoreArgument);
                }
                for (int i = 1; i <= 12; i++)
                {
                    var areaNumber = i.ToString();
                    Argument areaArgument = new(name: "A" + areaNumber, type: "string", required: false, isMulti: true, nameHuman: "-A" + areaNumber + " / --score_area_" + areaNumber + "_effects", section: "Area Effects");
                    dartsWledArguments.Add(areaArgument);
                }
                for (int i = 1; i <= 20; i++)
                {
                    var dartscore = i.ToString();
                    Argument dartscoreArgument = new(name: "DS" + dartscore, type: "string", required: false, isMulti: true, nameHuman: "-DS" + dartscore + " / --dart_score_" + dartscore + "_effects", section: "Single Dart Effects !! still in Progress !!");
                    dartsWledArguments.Add(dartscoreArgument);
                }

                AppDownloadable dartsWled =
                new(
                    downloadUrl: dartsWledDownloadUrl,
                    changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-wled/master/CHANGELOG.md",
                    name: "darts-wled",
                    helpUrl: "https://github.com/lbormann/darts-wled",
                    descriptionShort: "Controls WLED installations by autodarts-events",
                    configuration: new(
                        prefix: "-",
                        delimitter: " ",
                        arguments: dartsWledArguments)
                    );
                
                apps.Add(dartsWled);
                //foreach (var argument in dartsWled.Configuration.Arguments)
                //{
                //    if (argumentDescriptions.TryGetValue(argument.Name, out var description))
                //    {
                //        argument.Description = description;
                //    }
                //}
            }

            if (!string.IsNullOrEmpty(dartsPixelitDownloadUrl))
            {
                var dartsPixelitArguments = new List<Argument> {
                        new(name: "CON", type: "string", required: false, nameHuman: "-CON / --connection", section: "Service"),
                        new(name: "PEPS", type: "string", required: true, isMulti: true, nameHuman: "-PEPS / --pixelit_endpoints", section: "PIXELIT"),
                        new(name: "TP", type: "path", required: true, nameHuman: "-TP / --templates_path", section: "PIXELIT"),
                        new(name: "BRI", type: "int[1..255]", required: false, nameHuman: "-BRI / --effect_brightness", section: "PIXELIT"),
                        new(name: "HFO", type: "int[2..170]", required: false, nameHuman: "-HFO / --high_finish_on", section: "Autodarts"),
                        new(name: "HF", type: "string", required: false, isMulti: true, nameHuman: "-HF / --high_finish_effects", section: "PIXELIT"),
                        new(name: "AS", type: "string", required: false, isMulti: true, nameHuman: "-AS / --app_start_effects", section: "PIXELIT"),
                        new(name: "IDE", type: "string", required: false, isMulti: true, nameHuman: "-IDE / --idle_effects", section: "PIXELIT"),
                        new(name: "GS", type: "string", required: false, isMulti: true, nameHuman: "-GS / --game_start_effects", section: "PIXELIT"),
                        new(name: "MS", type: "string", required: false, isMulti: true, nameHuman: "-MS / --match_start_effects", section: "PIXELIT"),
                        new(name: "G", type: "string", required: false, isMulti: true, nameHuman: "-G / --game_won_effects", section: "PIXELIT"),
                        new(name: "M", type: "string", required: false, isMulti : true, nameHuman: "-M / --match_won_effects", section: "PIXELIT"),
                        new(name: "B", type: "string", required: false, isMulti : true, nameHuman: "-B / --busted_effects", section: "PIXELIT"),
                        new(name: "PJ", type: "string", required: false, isMulti : true, nameHuman: "-PJ / --player_joined_effects", section: "PIXELIT"),
                        new(name: "PL", type: "string", required: false, isMulti : true, nameHuman: "-PL / --player_left_effects", section: "PIXELIT"),
                        new(name: "DEB", type: "bool", required: false, nameHuman: "-DEB / --debug", section: "Service", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" })
                    };
                for (int i = 0; i <= 180; i++)
                {
                    var score = i.ToString();
                    Argument scoreArgument = new(name: "S" + score, type: "string", required: false, isMulti: true, nameHuman: "-S" + score + " / --score_" + score + "_effects", section: "PIXELIT");
                    dartsPixelitArguments.Add(scoreArgument);
                }
                for (int i = 1; i <= 12; i++)
                {
                    var areaNumber = i.ToString();
                    Argument areaArgument = new(name: "A" + areaNumber, type: "string", required: false, isMulti: true, nameHuman: "-A" + areaNumber + " / --score_area_" + areaNumber + "_effects", section: "PIXELIT");
                    dartsPixelitArguments.Add(areaArgument);
                }

                AppDownloadable dartsPixelit =
                new(
                    downloadUrl: dartsPixelitDownloadUrl,
                    changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-pixelit/main/CHANGELOG.md",
                    name: "darts-pixelit",
                    helpUrl: "https://github.com/lbormann/darts-pixelit",
                    descriptionShort: "Controls PIXELIT installations by autodarts-events",
                    configuration: new(
                        prefix: "-",
                        delimitter: " ",
                        arguments: dartsPixelitArguments)
                    );
                apps.Add(dartsPixelit);
            }

            if (!string.IsNullOrEmpty(dartsGifDownloadUrl))
            {
                var dartsGifArguments = new List<Argument> {
                         new(name: "MP", type: "path", required: false, nameHuman: "-MP / --media_path", section: "Media"),
                         new(name: "CON", type: "string", required: false, nameHuman: "-CON / --connection", section: "Service"),
                         new(name: "HFO", type: "int[2..170]", required: false, nameHuman: "-HFO / --high_finish_on", section: "Autodarts"),
                         new(name: "HF", type: "string", required: false, isMulti: true, nameHuman: "-HF / --high_finish_images", section: "Images"),
                         new(name: "G", type: "string", required: false, isMulti: true, nameHuman: "-G / --game_won_images", section: "Images"),
                         new(name: "M", type: "string", required: false, isMulti : true, nameHuman: "-M / --match_won_images", section: "Images"),
                         new(name: "B", type: "string", required: false, isMulti : true, nameHuman: "-B / --busted_images", section: "Images"),
                         new(name: "WEB", type: "int[0..2]", required: false, nameHuman: "-WEB / --web_gif", section: "Service"),
                         new(name: "WEBP", type: "int", required: false, nameHuman: "-WEBP / --web_gif_port", section: "Service"),
                         new(name: "DEB", type: "bool", required: false, nameHuman: "-DEB / --debug", section: "Service", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" })

                     };
                for (int i = 0; i <= 180; i++)
                {
                    var score = i.ToString();
                    Argument scoreArgument = new(name: "S" + score, type: "string", required: false, isMulti: true, nameHuman: "-S" + score + " / --score_" + score + "_images", section: "Images");
                    dartsGifArguments.Add(scoreArgument);
                }
                for (int i = 1; i <= 12; i++)
                {
                    var areaNumber = i.ToString();
                    Argument areaArgument = new(name: "A" + areaNumber, type: "string", required: false, isMulti: true, nameHuman: "-A" + areaNumber + " / --score_area_" + areaNumber + "_images", section: "Images");
                    dartsGifArguments.Add(areaArgument);
                }

                AppDownloadable dartsGif =
                new(
                    downloadUrl: dartsGifDownloadUrl,
                    changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-gif/main/CHANGELOG.md",
                    name: "darts-gif",
                    helpUrl: "https://github.com/lbormann/darts-gif",
                    descriptionShort: "Displays images according to autodarts-events",
                    configuration: new(
                        prefix: "-",
                        delimitter: " ",
                        arguments: dartsGifArguments)
                    );
                apps.Add(dartsGif);
            }

            if (!string.IsNullOrEmpty(dartsVoiceDownloadUrl))
            {
                var dartsVoiceArguments = new List<Argument> {
                        new(name: "CON", type: "string", required: false, nameHuman: "-CON / --connection", section: "Service"),
                        new(name: "MP", type: "path", required: true, nameHuman: "-MP / --model_path", section: "Voice-Recognition"),
                        new(name: "L", type: "int[0..2]", required: false, nameHuman: "-L / --language", section: "Voice-Recognition"),
                        new(name: "KNG", type: "string", required: false, isMulti: true, nameHuman: "-KNG / --keywords_next_game", section: "Voice-Recognition"),
                        new(name: "KN", type: "string", required: false, isMulti: true, nameHuman: "-KN / --keywords_next", section: "Voice-Recognition"),
                        new(name: "KU", type: "string", required: false, isMulti: true, nameHuman: "-KU / --keywords_undo", section: "Voice-Recognition"),
                        new(name: "KBC", type: "string", required: false, isMulti: true, nameHuman: "-KBC / --keywords_ban_caller", section: "Voice-Recognition"),
                        new(name: "KCC", type: "string", required: false, isMulti: true, nameHuman: "-KCC / --keywords_change_caller", section: "Voice-Recognition"),
                        new(name: "KSB", type: "string", required: false, isMulti: true, nameHuman: "-KSB / --keywords_start_board", section: "Voice-Recognition"),
                        new(name: "KSPB", type: "string", required: false, isMulti: true, nameHuman: "-KSPB / --keywords_stop_board", section: "Voice-Recognition"),
                        new(name: "KRB", type: "string", required: false, isMulti: true, nameHuman: "-KRB / --keywords_reset_board", section: "Voice-Recognition"),
                        new(name: "KCB", type: "string", required: false, isMulti: true, nameHuman: "-KCB / --keywords_calibrate_board", section: "Voice-Recognition"),
                        new(name: "KFD", type: "string", required: false, isMulti: true, nameHuman: "-KFD / --keywords_first_dart", section: "Voice-Recognition"),
                        new(name: "KSD", type: "string", required: false, isMulti: true, nameHuman: "-KSD / --keywords_second_dart", section: "Voice-Recognition"),
                        new(name: "KTD", type: "string", required: false, isMulti: true, nameHuman: "-KTD / --keywords_third_dart", section: "Voice-Recognition"),
                        new(name: "KS", type: "string", required: false, isMulti: true, nameHuman: "-KS / --keywords_single", section: "Voice-Recognition"),
                        new(name: "KD", type: "string", required: false, isMulti: true, nameHuman: "-KD / --keywords_double", section: "Voice-Recognition"),
                        new(name: "KT", type: "string", required: false, isMulti: true, nameHuman: "-KT / --keywords_triple", section: "Voice-Recognition"),
                        new(name: "KZERO", type: "string", required: false, isMulti: true, nameHuman: "-KZERO / --keywords_zero", section: "Voice-Recognition"),
                        new(name: "KONE", type: "string", required: false, isMulti: true, nameHuman: "-KONE / --keywords_one", section: "Voice-Recognition"),
                        new(name: "KTWO", type: "string", required: false, isMulti: true, nameHuman: "-KTWO / --keywords_two", section: "Voice-Recognition"),
                        new(name: "KTHREE", type: "string", required: false, isMulti: true, nameHuman: "-KTHREE / --keywords_three", section: "Voice-Recognition"),
                        new(name: "KFOUR", type: "string", required: false, isMulti: true, nameHuman: "-KFOUR / --keywords_four", section: "Voice-Recognition"),
                        new(name: "KFIVE", type: "string", required: false, isMulti: true, nameHuman: "-KFIVE / --keywords_five", section: "Voice-Recognition"),
                        new(name: "KSIX", type: "string", required: false, isMulti: true, nameHuman: "-KSIX / --keywords_six", section: "Voice-Recognition"),
                        new(name: "KSEVEN", type: "string", required: false, isMulti: true, nameHuman: "-KSEVEN / --keywords_seven", section: "Voice-Recognition"),
                        new(name: "KEIGHT", type: "string", required: false, isMulti: true, nameHuman: "-KEIGHT / --keywords_eight", section: "Voice-Recognition"),
                        new(name: "KNINE", type: "string", required: false, isMulti: true, nameHuman: "-KNINE / --keywords_nine", section: "Voice-Recognition"),
                        new(name: "KTEN", type: "string", required: false, isMulti: true, nameHuman: "-KTEN / --keywords_ten", section: "Voice-Recognition"),
                        new(name: "KELEVEN", type: "string", required: false, isMulti: true, nameHuman: "-KELEVEN / --keywords_eleven", section: "Voice-Recognition"),
                        new(name: "KTWELVE", type: "string", required: false, isMulti: true, nameHuman: "-KTWELVE / --keywords_twelve", section: "Voice-Recognition"),
                        new(name: "KTHIRTEEN", type: "string", required: false, isMulti: true, nameHuman: "-KTHIRTEEN / --keywords_thirteen", section: "Voice-Recognition"),
                        new(name: "KFOURTEEN", type: "string", required: false, isMulti: true, nameHuman: "-KFOURTEEN / --keywords_fourteen", section: "Voice-Recognition"),
                        new(name: "KFIFTEEN", type: "string", required: false, isMulti: true, nameHuman: "-KFIFTEEN / --keywords_fifteen", section: "Voice-Recognition"),
                        new(name: "KSIXTEEN", type: "string", required: false, isMulti: true, nameHuman: "-KSIXTEEN / --keywords_sixteen", section: "Voice-Recognition"),
                        new(name: "KSEVENTEEN", type: "string", required: false, isMulti: true, nameHuman: "-KSEVENTEEN / --keywords_seventeen", section: "Voice-Recognition"),
                        new(name: "KEIGHTEEN", type: "string", required: false, isMulti: true, nameHuman: "-KEIGHTEEN / --keywords_eighteen", section: "Voice-Recognition"),
                        new(name: "KNINETEEN", type: "string", required: false, isMulti: true, nameHuman: "-KNINETEEN / --keywords_nineteen", section: "Voice-Recognition"),
                        new(name: "KTWENTY", type: "string", required: false, isMulti: true, nameHuman: "-KTWENTY / --keywords_twenty", section: "Voice-Recognition"),
                        new(name: "KTWENTYFIVE", type: "string", required: false, isMulti: true, nameHuman: "-KTWENTY_FIVE / --keywords_twenty_five", section: "Voice-Recognition"),
                        new(name: "KFIFTY", type: "string", required: false, isMulti: true, nameHuman: "-KFIFTY / --keywords_fifty", section: "Voice-Recognition"),
                        new(name: "DEB", type: "bool", required: false, nameHuman: "-DEB / --debug", section: "Service", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" })
                    };

                AppDownloadable dartsVoice =
                new(
                    downloadUrl: dartsVoiceDownloadUrl,
                    changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-voice/main/CHANGELOG.md",
                    name: "darts-voice",
                    helpUrl: "https://github.com/lbormann/darts-voice",
                    descriptionShort: "Controls autodarts by using your voice",
                    configuration: new(
                        prefix: "-",
                        delimitter: " ",
                        arguments: dartsVoiceArguments)
                    );
                apps.Add(dartsVoice);
            }

            if (!string.IsNullOrEmpty(camLoaderDownloadUrl))
            {
                AppDownloadable camLoader =
                new(
                    downloadUrl: camLoaderDownloadUrl,
                    changelogUrl: "https://raw.githubusercontent.com/lbormann/cam-loader/master/CHANGELOG.md",
                    name: "cam-loader",
                    helpUrl: "https://github.com/lbormann/cam-loader",
                    descriptionShort: "Saves and loads settings for multiple cameras"
                    );
                apps.Add(camLoader);
            }

            if (!string.IsNullOrEmpty(virtualDartsZoomDownloadUrl))
            {
                AppDownloadable virtualDartsZoom =
                new(
                    downloadUrl: virtualDartsZoomDownloadUrl,
                    name: "virtual-darts-zoom",
                    helpUrl: "https://lehmann-bo.de/?p=28",
                    descriptionShort: "Zooms webcam-image onto thrown darts",
                    runAsAdmin: true
                    );
                apps.Add(virtualDartsZoom);
            }


            AppsDownloadable.AddRange(apps);
            AppsAll.AddRange(apps);
            
            SerializeApps(apps, appsDownloadableFile);
        }

        private async void MigrateAppsDownloadable()
        {
            //var readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-caller/refs/heads/master/README.md"; // URL zur README-Datei
            //var parser = new ReadmeParser();
            //var argumentDescriptions = await parser.GetArgumentsFromReadme(readmeUrl);
            var dartsCaller = AppsDownloadable.Find(a => a.Name == "darts-caller");
            if (dartsCaller != null)
            {
                if (dartsCallerDownloadUrl != null)
                {
                    dartsCaller.DownloadUrl = dartsCallerDownloadUrl;

                    var callEveryDart = dartsCaller.Configuration.Arguments.Find(a => a.Name == "E");
                    if (callEveryDart != null)
                    {
                        if (callEveryDart.Type == "int[0..2]")
                        {
                            if (callEveryDart.Value == "2")
                            {
                                callEveryDart.Value = "3";
                            }
                            callEveryDart.Type = "int[0..3]";
                            callEveryDart.ValidateType();
                        }
                    }


                    var callBotActions = dartsCaller.Configuration.Arguments.Find(a => a.Name == "CBA");
                    if (callBotActions == null)
                    {
                        dartsCaller.Configuration.Arguments.Add(new(name: "CBA", type: "bool", required: false, nameHuman: "-CBA / --call_bot_actions", section: "Calls", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }));
                    }
                    var removeOldVoicePacks = dartsCaller.Configuration.Arguments.Find(a => a.Name == "ROVP");
                    if (removeOldVoicePacks == null)
                    {
                        dartsCaller.Configuration.Arguments.Add(new(name: "ROVP", type: "bool", required: false, nameHuman: "-ROVP / --remove_old_voice_packs", section: "Downloads", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }));
                    }
                    var callerRealLife = dartsCaller.Configuration.Arguments.Find(a => a.Name == "CRL");
                    if (callerRealLife == null)
                    {
                        dartsCaller.Configuration.Arguments.Add(new(name: "CRL", type: "bool", required: false, nameHuman: "-CRL / --caller_real_life", section: "Calls", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }));
                    }
                    var callBlindSupport = dartsCaller.Configuration.Arguments.Find(a => a.Name == "CBS");
                    if (callBlindSupport == null)
                    {
                        dartsCaller.Configuration.Arguments.Add(new(name: "CBS", type: "bool", required: false, nameHuman: "-CBS / --call_blind_support", section: "Calls", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }));
                    }




                }
                else
                {
                    var dartsCallerIndex = AppsDownloadable.FindIndex(a => a.Name == "darts-caller");
                    if (dartsCallerIndex != -1)
                    {
                        AppsDownloadable.RemoveAt(dartsCallerIndex);
                    }
                }
            }

            // Migration of darts-WLED
            var dartsWled = AppsDownloadable.Find(a => a.Name == "darts-wled");
            if (dartsWled != null)
            {
                if (dartsWledDownloadUrl != null)
                {
                    //readmeUrl = "https://raw.githubusercontent.com/lbormann/darts-wled/refs/heads/main/README.md";
                    //argumentDescriptions = await parser.GetArgumentsFromReadme(readmeUrl);
                    dartsWled.DownloadUrl = dartsWledDownloadUrl;

                    var boardStopAfterWin = dartsWled.Configuration.Arguments.Find(a => a.Name == "BSW");
                    if (boardStopAfterWin == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "BSW", type: "bool", required: false, nameHuman: "-BSW / --board_stop_after_win", section: "Board Stop Start", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }));
                    }
                    var boardStopeffect = dartsWled.Configuration.Arguments.Find(a => a.Name == "BSE");
                    if (boardStopeffect == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "BSE", type: "string", required: false, isMulti: true, nameHuman: "-BSE / --board_stop_effect", section: "Board Status Effects"));
                    }
                    var takeouteffect = dartsWled.Configuration.Arguments.Find(a => a.Name == "TOE");
                    if (takeouteffect == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "TOE", type: "string", required: false, isMulti: true, nameHuman: "-TOE / --takeout_effect", section: "Board Status Effects"));
                    }
                    var calibeffect = dartsWled.Configuration.Arguments.Find(a => a.Name == "CE");
                    if (calibeffect == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "CE", type: "string", required: false, isMulti: true, nameHuman: "-CE / --calibration_effect", section: "Board Status Effects"));
                    }
                    var wledsoff = dartsWled.Configuration.Arguments.Find(a => a.Name == "SOFF");
                    if (wledsoff == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "SOFF", type: "bool", required: false, nameHuman: "-SOFF / --wled_off_at_start", section: "Service", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }));
                    }                    
                    var wledoff = dartsWled.Configuration.Arguments.Find(a => a.Name == "OFF");
                    if (wledoff == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "OFF", type: "bool", required: false, nameHuman: "-OFF / --wled_off", section: "Service", valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }));
                    }
                    var wledds1 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS1");
                    if (wledds1 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS1", type: "string", required: false, isMulti: true, nameHuman: "-DS1 / --dart_score_1_effects", section: "Single Dart Effects!! still in Progress!!"));
                    }
                    var wledds2 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS2");
                    if (wledds2 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS2", type: "string", required: false, isMulti: true, nameHuman: "-DS2 / --dart_score_2_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds3 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS3");
                    if (wledds3 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS3", type: "string", required: false, isMulti: true, nameHuman: "-DS3 / --dart_score_3_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds4 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS4");
                    if (wledds4 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS4", type: "string", required: false, isMulti: true, nameHuman: "-DS4 / --dart_score_4_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds5 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS5");
                    if (wledds5 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS5", type: "string", required: false, isMulti: true, nameHuman: "-DS5 / --dart_score_5_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds6 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS6");
                    if (wledds6 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS6", type: "string", required: false, isMulti: true, nameHuman: "-DS6 / --dart_score_6_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds7 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS7");
                    if (wledds7 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS7", type: "string", required: false, isMulti: true, nameHuman: "-DS7 / --dart_score_7_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds8 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS8");
                    if (wledds8 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS8", type: "string", required: false, isMulti: true, nameHuman: "-DS8 / --dart_score_8_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds9 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS9");
                    if (wledds9 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS9", type: "string", required: false, isMulti: true, nameHuman: "-DS9 / --dart_score_9_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds10 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS10");
                    if (wledds10 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS10", type: "string", required: false, isMulti: true, nameHuman: "-DS10 / --dart_score_10_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds11 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS11");
                    if (wledds11 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS11", type: "string", required: false, isMulti: true, nameHuman: "-DS11 / --dart_score_11_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds12 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS12");
                    if (wledds12 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS12", type: "string", required: false, isMulti: true, nameHuman: "-DS12 / --dart_score_12_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds13 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS13");
                    if (wledds13 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS13", type: "string", required: false, isMulti: true, nameHuman: "-DS13 / --dart_score_13_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds14 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS14");
                    if (wledds14 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS14", type: "string", required: false, isMulti: true, nameHuman: "-DS14 / --dart_score_14_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds15 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS15");
                    if (wledds15 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS15", type: "string", required: false, isMulti: true, nameHuman: "-DS15 / --dart_score_15_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds16 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS16");
                    if (wledds16 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS16", type: "string", required: false, isMulti: true, nameHuman: "-DS16 / --dart_score_16_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds17 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS17");
                    if (wledds17 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS17", type: "string", required: false, isMulti: true, nameHuman: "-DS17 / --dart_score_17_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds18 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS18");
                    if (wledds18 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS18", type: "string", required: false, isMulti: true, nameHuman: "-DS18 / --dart_score_18_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds19 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS19");
                    if (wledds19 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS19", type: "string", required: false, isMulti: true, nameHuman: "-DS19 / --dart_score_19_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledds20 = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS20");
                    if (wledds20 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DS20", type: "string", required: false, isMulti: true, nameHuman: "-DS20 / --dart_score_20_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledDSbull = dartsWled.Configuration.Arguments.Find(a => a.Name == "DSBULL");
                    if (wledDSbull == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "DSBULL", type: "string", required: false, isMulti: true, nameHuman: "-DSBULL / --dart_score_BULL_effects", section: "Single Dart Effects !! still in Progress !!"));
                    }
                    var wledIDE2 = dartsWled.Configuration.Arguments.Find(a => a.Name == "IDE2");
                    if (wledIDE2 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "IDE2", type: "string", required: false, nameHuman: "-IDE2 / --idle_effect_player2", section: "Service"));
                    }
                    var wledIDE3 = dartsWled.Configuration.Arguments.Find(a => a.Name == "IDE3");
                    if (wledIDE3 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "IDE3", type: "string", required: false, nameHuman: "-IDE3 / --idle_effect_player3", section: "Service"));
                    }
                    var wledIDE4 = dartsWled.Configuration.Arguments.Find(a => a.Name == "IDE4");
                    if (wledIDE4 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "IDE4", type: "string", required: false, nameHuman: "-IDE4 / --idle_effect_player4", section: "Service"));
                    }
                    var wledIDE5 = dartsWled.Configuration.Arguments.Find(a => a.Name == "IDE5");
                    if (wledIDE5 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "IDE5", type: "string", required: false, nameHuman: "-IDE5 / --idle_effect_player5", section: "Service"));
                    }
                    var wledIDE6 = dartsWled.Configuration.Arguments.Find(a => a.Name == "IDE6");
                    if (wledIDE6 == null)
                    {
                        dartsWled.Configuration.Arguments.Add(new(name: "IDE6", type: "string", required: false, nameHuman: "-IDE6 / --idle_effect_player6", section: "Service"));
                    }




                    for (int i = 0; i <= 180; i++)
                    {
                        var argS = i.ToString();
                        var WLEDDescMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "S" + i);
                        if ( WLEDDescMig != null) { WLEDDescMig.Section ="Score Effects" ; }
                    }
                    for (int i = 1; i <= 12; i++)
                    {
                        var argA = i.ToString();
                        var WLEDDescMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "A" + i);
                        if (WLEDDescMig != null) { WLEDDescMig.Section = "Area Effects"; }
                    }
                    for (int i = 1; i <= 20; i++)
                    {
                        var argA = i.ToString();
                        var WLEDDescMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "DS" + i);
                        if (WLEDDescMig != null) { WLEDDescMig.Description = "NOT FULLY IMPLEMENTED!!!!!!";
                            WLEDDescMig.Section = "Single Dart Effects !! still in Progress !!";
                        }
                    }
                    var DSBULLWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "DSBULL");
                    if (DSBULLWLEDMig != null) { DSBULLWLEDMig.Description = "NOT FULLY IMPLEMENTED!!!!!!";
                        DSBULLWLEDMig.Section = "Single Dart Effects !! still in Progress !!";
                    }

                    var DEBWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "DEB");
                    if (DEBWLEDMig != null) { DEBWLEDMig.Section = "Service"; }
                    var CONWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "CON");
                    if (CONWLEDMig != null) { CONWLEDMig.Section = "Service"; }
                    var WEPSWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "WEPS");
                    if (WEPSWLEDMig != null) { WEPSWLEDMig.Section = "Service"; }
                    var DUWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "DU");
                    if (DUWLEDMig != null) 
                    { 
                        DUWLEDMig.Section = "Board Stop Start";
                        // Migrate old type to new range
                        if (DUWLEDMig.Type == "int[0..10]")
                        {
                            DUWLEDMig.Type = "int[0..1000]";
                            DUWLEDMig.ValidateType();
                            System.Diagnostics.Debug.WriteLine("[Migration] Updated DU argument type from int[0..10] to int[0..1000]");
                        }
                    }
                    var BSSWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "BSS");
                    if (BSSWLEDMig != null) { BSSWLEDMig.Section = "Board Stop Start"; }
                    var BRIWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "BRI");
                    if (BRIWLEDMig != null) { BRIWLEDMig.Section = "Service"; }
                    var HFOWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "HFO");
                    if (HFOWLEDMig != null) { HFOWLEDMig.Section = "Effects"; }
                    var HFWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "HF");
                    if (HFWLEDMig != null) { HFWLEDMig.Section = "Effects"; }
                    var IDEWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "IDE");
                    if (IDEWLEDMig != null) { IDEWLEDMig.Section = "Service"; }
                    var GWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "G");
                    if (GWLEDMig != null) { GWLEDMig.Section = "Effects"; }
                    var MWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "M");
                    if (MWLEDMig != null) { MWLEDMig.Section = "Effects"; }
                    var BWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "B");
                    if (BWLEDMig != null) { BWLEDMig.Section = "Effects"; }
                    var PJWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "PJ");
                    if (PJWLEDMig != null) { PJWLEDMig.Section = "Lobby Effects"; }
                    var PLWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "PL");
                    if (PLWLEDMig != null) { PLWLEDMig.Section = "Lobby Effects"; }
                    var BSWWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "BSW");
                    if (BSWWLEDMig != null) { BSWWLEDMig.Section = "Board Stop Start"; }
                    var BSEWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "BSE");
                    if (BSEWLEDMig != null) { BSEWLEDMig.Section = "Board Status Effects"; }
                    var TOEWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "TOE");
                    if (TOEWLEDMig != null) { TOEWLEDMig.Section = "Board Status Effects"; }
                    var CEWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "CE");
                    if (CEWLEDMig != null) { CEWLEDMig.Section = "Board Status Effects"; }
                    var OFFWLEDMig = dartsWled.Configuration.Arguments.Find(a => a.Name == "OFF");
                    if (OFFWLEDMig != null) { OFFWLEDMig.Section = "Service"; }



                    //var wledtest = dartsWled.Configuration.Arguments.Find(a => a.Name == "TEST");
                    //if (wledtest != null)
                    //{
                    //    dartsWled.Configuration.Arguments.Remove(wledtest);
                    //}
                }
            }
            var dartsPixelit = AppsDownloadable.Find(a => a.Name == "darts-pixelit");
            if (dartsPixelit != null)
            {
                if (dartsPixelitDownloadUrl != null)
                {
                    dartsPixelit.DownloadUrl = dartsPixelitDownloadUrl;

                    // Add more migs..
                }
            }
        }


        private void CreateDummyProfiles()
        {
            // INSTALLABLE
            var dartboardsClient = AppsInstallable.Find(a => a.Name == "dartboards-client") != null;
            var droidCam = AppsInstallable.Find(a => a.Name == "droid-cam") != null;
            var epocCam = AppsInstallable.Find(a => a.Name == "epoc-cam") != null;
            
            // DOWNLOADABLE
            var dartsCaller = AppsDownloadable.Find(a => a.Name == "darts-caller") != null;
            var dartsExtern = AppsDownloadable.Find(a => a.Name == "darts-extern") != null;
            var dartsWled = AppsDownloadable.Find(a => a.Name == "darts-wled") != null;
            var dartsPixelit = AppsDownloadable.Find(a => a.Name == "darts-pixelit") != null;
            var dartsGif = AppsDownloadable.Find(a => a.Name == "darts-gif") != null;
            var dartsVoice = AppsDownloadable.Find(a => a.Name == "darts-voice") != null;
            var camLoader = AppsDownloadable.Find(a => a.Name == "cam-loader") != null;
            var virtualDartsZoom = AppsDownloadable.Find(a => a.Name == "virtual-darts-zoom") != null;

            // LOCAL
            var custom1 = AppsLocal.Find(a => a.Name == "custom-1") != null;
            var custom2 = AppsLocal.Find(a => a.Name == "custom-2") != null;
            var custom3 = AppsLocal.Find(a => a.Name == "custom-3") != null;
            var custom4 = AppsLocal.Find(a => a.Name == "custom-4") != null;
            var custom5 = AppsLocal.Find(a => a.Name == "custom-5") != null;

            // OPEN
            var customUrl1 = AppsOpen.Find(a => a.Name == "custom-url-1") != null;
            var customUrl2 = AppsOpen.Find(a => a.Name == "custom-url-2") != null;
            var customUrl3 = AppsOpen.Find(a => a.Name == "custom-url-3") != null;
            var customUrl4 = AppsOpen.Find(a => a.Name == "custom-url-4") != null;
            var customUrl5 = AppsOpen.Find(a => a.Name == "custom-url-5") != null;

            if (dartsCaller)
            {
                var p1Name = "darts-caller";
                var p1Apps = new Dictionary<string, ProfileState>();
                if (dartsCaller) p1Apps.Add("darts-caller", new ProfileState(false, true)); // Changed: isRequired=false, taggedForStart=true
                if (dartsWled) p1Apps.Add("darts-wled", new ProfileState());
                if (dartsPixelit) p1Apps.Add("darts-pixelit", new ProfileState());
                if (dartsGif) p1Apps.Add("darts-gif", new ProfileState());
                if (dartsVoice) p1Apps.Add("darts-voice", new ProfileState());
                if (camLoader) p1Apps.Add("cam-loader", new ProfileState());
                if (custom1) p1Apps.Add("custom-1", new ProfileState());
                if (custom2) p1Apps.Add("custom-2", new ProfileState());
                if (custom3) p1Apps.Add("custom-3", new ProfileState());
                if (custom4) p1Apps.Add("custom-4", new ProfileState());
                if (custom5) p1Apps.Add("custom-5", new ProfileState());
                if (customUrl1) p1Apps.Add("custom-url-1", new ProfileState());
                if (customUrl2) p1Apps.Add("custom-url-2", new ProfileState());
                if (customUrl3) p1Apps.Add("custom-url-3", new ProfileState());
                if (customUrl4) p1Apps.Add("custom-url-4", new ProfileState());
                if (customUrl5) p1Apps.Add("custom-url-5", new ProfileState());
                Profiles.Add(new Profile(p1Name, p1Apps));
            }
            
            if (dartsCaller && dartsExtern)
            {
                var p2Name = "darts-extern: lidarts.org";
                var p2Args = new Dictionary<string, string> { { "extern_platform", "lidarts" } };
                var p2Apps = new Dictionary<string, ProfileState>();
                if (dartsCaller) p2Apps.Add("darts-caller", new ProfileState(false, true)); // Changed: isRequired=false, taggedForStart=true
                if (dartsWled) p2Apps.Add("darts-wled", new ProfileState());
                if (dartsPixelit) p2Apps.Add("darts-pixelit", new ProfileState());
                if (dartsGif) p2Apps.Add("darts-gif", new ProfileState());
                if (dartsVoice) p2Apps.Add("darts-voice", new ProfileState());
                if (dartsExtern) p2Apps.Add("darts-extern", new ProfileState(true, true, runtimeArguments: p2Args)); // extern stays required
                if (virtualDartsZoom) p2Apps.Add("virtual-darts-zoom", new ProfileState());
                if (camLoader) p2Apps.Add("cam-loader", new ProfileState());
                if (droidCam) p2Apps.Add("droid-cam", new ProfileState());
                if (epocCam) p2Apps.Add("epoc-cam", new ProfileState());
                if (custom1) p2Apps.Add("custom-1", new ProfileState());
                if (custom2) p2Apps.Add("custom-2", new ProfileState());
                if (custom3) p2Apps.Add("custom-3", new ProfileState());
                if (custom4) p2Apps.Add("custom-4", new ProfileState());
                if (custom5) p2Apps.Add("custom-5", new ProfileState());
                if (customUrl1) p2Apps.Add("custom-url-1", new ProfileState());
                if (customUrl2) p2Apps.Add("custom-url-2", new ProfileState());
                if (customUrl3) p2Apps.Add("custom-url-3", new ProfileState());
                if (customUrl4) p2Apps.Add("custom-url-4", new ProfileState());
                if (customUrl5) p2Apps.Add("custom-url-5", new ProfileState());
                Profiles.Add(new Profile(p2Name, p2Apps));
            }

            if (dartsCaller && dartsExtern)
            {
                var p3Name = "darts-extern: nakka.com/n01/online";
                var p3Args = new Dictionary<string, string> { { "extern_platform", "nakka" } };
                var p3Apps = new Dictionary<string, ProfileState>();
                if (dartsCaller) p3Apps.Add("darts-caller", new ProfileState(false, true)); // Changed: isRequired=false, taggedForStart=true
                if (dartsWled) p3Apps.Add("darts-wled", new ProfileState());
                if (dartsPixelit) p3Apps.Add("darts-pixelit", new ProfileState());
                if (dartsGif) p3Apps.Add("darts-gif", new ProfileState());
                if (dartsVoice) p3Apps.Add("darts-voice", new ProfileState());
                if (dartsExtern) p3Apps.Add("darts-extern", new ProfileState(true, true, runtimeArguments: p3Args)); // extern stays required
                if (virtualDartsZoom) p3Apps.Add("virtual-darts-zoom", new ProfileState());
                if (camLoader) p3Apps.Add("cam-loader", new ProfileState());
                if (droidCam) p3Apps.Add("droid-cam", new ProfileState());
                if (epocCam) p3Apps.Add("epoc-cam", new ProfileState());
                if (custom1) p3Apps.Add("custom-1", new ProfileState());
                if (custom2) p3Apps.Add("custom-2", new ProfileState());
                if (custom3) p3Apps.Add("custom-3", new ProfileState());
                if (custom4) p3Apps.Add("custom-4", new ProfileState());
                if (custom5) p3Apps.Add("custom-5", new ProfileState());
                if (customUrl1) p3Apps.Add("custom-url-1", new ProfileState());
                if (customUrl2) p3Apps.Add("custom-url-2", new ProfileState());
                if (customUrl3) p3Apps.Add("custom-url-3", new ProfileState());
                if (customUrl4) p3Apps.Add("custom-url-4", new ProfileState());
                if (customUrl5) p3Apps.Add("custom-url-5", new ProfileState());
                Profiles.Add(new Profile(p3Name, p3Apps));
            }

            if (dartsCaller && dartsExtern)
            {
                var p4Name = "darts-extern: dartboards.online";
                var p4Args = new Dictionary<string, string> { { "extern_platform", "dartboards" } };
                var p4Apps = new Dictionary<string, ProfileState>();
                if (dartsCaller) p4Apps.Add("darts-caller", new ProfileState(false, true)); // Changed: isRequired=false, taggedForStart=true
                if (dartsWled) p4Apps.Add("darts-wled", new ProfileState());
                if (dartsPixelit) p4Apps.Add("darts-pixelit", new ProfileState());
                if (dartsGif) p4Apps.Add("darts-gif", new ProfileState());
                if (dartsVoice) p4Apps.Add("darts-voice", new ProfileState());
                if (dartsExtern) p4Apps.Add("darts-extern", new ProfileState(true, true, runtimeArguments: p4Args)); // extern stays required
                if (virtualDartsZoom) p4Apps.Add("virtual-darts-zoom", new ProfileState());
                if (camLoader) p4Apps.Add("cam-loader", new ProfileState());
                if (dartboardsClient) p4Apps.Add("dartboards-client", new ProfileState());
                if (droidCam) p4Apps.Add("droid-cam", new ProfileState());
                if (epocCam) p4Apps.Add("epoc-cam", new ProfileState());
                if (custom1) p4Apps.Add("custom-1", new ProfileState());
                if (custom2) p4Apps.Add("custom-2", new ProfileState());
                if (custom3) p4Apps.Add("custom-3", new ProfileState());
                if (custom4) p4Apps.Add("custom-4", new ProfileState());
                if (custom5) p4Apps.Add("custom-5", new ProfileState());
                if (customUrl1) p4Apps.Add("custom-url-1", new ProfileState());
                if (customUrl2) p4Apps.Add("custom-url-2", new ProfileState());
                if (customUrl3) p4Apps.Add("custom-url-3", new ProfileState());
                if (customUrl4) p4Apps.Add("custom-url-4", new ProfileState());
                if (customUrl5) p4Apps.Add("custom-url-5", new ProfileState());
                Profiles.Add(new Profile(p4Name, p4Apps));
            }


            SerializeProfiles(Profiles, profilesFile);
        }

        private void MigrateProfiles()
        {
            // Add more migs..
        }



        private void SerializeApps<AppBase>(List<AppBase> apps, string filename)
        {
            var settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            var appsJsonStr = JsonConvert.SerializeObject(apps, Formatting.Indented, settings);
            File.WriteAllText(filename, appsJsonStr);
        }
        
        private void SerializeProfiles(List<Profile> profiles, string filename)
        {
            var settings = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            };
            var profilesJsonStr = JsonConvert.SerializeObject(profiles, Formatting.Indented, settings);
            File.WriteAllText(filename, profilesJsonStr);
        }




        private void AppDownloadable_DownloadStarted(object? sender, AppEventArgs e)
        {
            OnAppDownloadStarted(e);
        }

        private void AppDownloadable_DownloadFinished(object? sender, AppEventArgs e)
        {
            OnAppDownloadFinished(e);
        }

        private void AppDownloadable_DownloadFailed(object? sender, AppEventArgs e)
        {
            OnAppDownloadFailed(e);
        }

        private void AppDownloadable_DownloadProgressed(object? sender, DownloadProgressChangedEventArgs e)
        {
            OnAppDownloadProgressed(e);
        }



        private void AppInstallable_InstallStarted(object? sender, AppEventArgs e)
        {
            OnAppInstallStarted(e);
        }

        private void AppInstallable_InstallFinished(object? sender, AppEventArgs e)
        {
            OnAppInstallFinished(e);
        }

        private void AppInstallable_InstallFailed(object? sender, AppEventArgs e)
        {
            OnAppInstallFailed(e);
        }

        private void App_AppConfigurationRequired(object? sender, AppEventArgs e)
        {
            OnAppConfigurationRequired(e);
        }



        protected virtual void OnAppDownloadStarted(AppEventArgs e)
        {
            AppDownloadStarted?.Invoke(this, e);
        }

        protected virtual void OnAppDownloadFinished(AppEventArgs e)
        {
            AppDownloadFinished?.Invoke(this, e);
        }

        protected virtual void OnAppDownloadFailed(AppEventArgs e)
        {
            AppDownloadFailed?.Invoke(this, e);
        }

        protected virtual void OnAppDownloadProgressed(DownloadProgressChangedEventArgs e)
        {
            AppDownloadProgressed?.Invoke(this, e);
        }



        protected virtual void OnAppInstallStarted(AppEventArgs e)
        {
            AppInstallStarted?.Invoke(this, e);
        }

        protected virtual void OnAppInstallFinished(AppEventArgs e)
        {
            AppInstallFinished?.Invoke(this, e);
        }

        protected virtual void OnAppInstallFailed(AppEventArgs e)
        {
            AppInstallFailed?.Invoke(this, e);
        }

        protected virtual void OnAppConfigurationRequired(AppEventArgs e)
        {
            AppConfigurationRequired?.Invoke(this, e);
        }
        

    }
}
