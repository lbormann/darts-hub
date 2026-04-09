using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;


namespace darts_hub.control
{
    public class AppConfiguration
    {
        public bool StartProfileOnStart { get; set; }
        public bool SkipUpdateConfirmation { get; set; }
        public bool IsBetaTester { get; set; }
        public bool NewSettingsMode { get; set; }
        public bool WizardCompleted { get; set; }
        public bool ShowRobbel3DSetup { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public int SplashCountdownSeconds { get; set; } = 1;

        // Window layout persistence
        public double WindowWidth { get; set; }
        public double WindowHeight { get; set; }
        public double WindowX { get; set; } = double.NaN;
        public double WindowY { get; set; } = double.NaN;
        public double NavColumnWidth { get; set; }
        public double TooltipColumnWidth { get; set; }

        // Monitor preference
        public bool UseSpecificMonitor { get; set; }
        public int PreferredMonitorIndex { get; set; }
    }



    public class Configurator
    {
        // ATTRIBUTES
        private readonly string ConfigFilePath;
        public AppConfiguration Settings { get; private set; }
        public bool RequiresRestart { get; private set; }



        // METHODS
        public Configurator(string configFileName) 
        {
            ConfigFilePath = Path.Combine(Helper.GetAppBasePath(), configFileName);
            LoadSettings();
        }

        public void SaveSettings()
        {
            var json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
        
        public void SetSetupWizardCompleted(bool completed)
        {
            Settings.WizardCompleted = completed;
            SaveSettings();
            System.Diagnostics.Debug.WriteLine($"[Configurator] Setup wizard completed status set to: {completed}");
        }


        private void LoadSettings()
        {
            if (!File.Exists(ConfigFilePath))
            {
                Settings = CreateDefaultSettings();
                SaveSettings();
                return;
            }

            var json = File.ReadAllText(ConfigFilePath);
            var parsedSettings = JObject.Parse(json);
            var hasRobbel3DFlag = parsedSettings.TryGetValue(nameof(AppConfiguration.ShowRobbel3DSetup), StringComparison.OrdinalIgnoreCase, out var robbel3DToken);
            var previousRobbel3DValue = robbel3DToken?.Value<bool?>();

            Settings = JsonConvert.DeserializeObject<AppConfiguration>(json) ?? CreateDefaultSettings();

            var settingsUpdated = false;

            // Ensure NewSettingsMode property exists (for backward compatibility)
            if (parsedSettings.Property(nameof(AppConfiguration.NewSettingsMode), StringComparison.OrdinalIgnoreCase) == null)
            {
                Settings.NewSettingsMode = false;
                settingsUpdated = true;
            }
            
            // Ensure WizardCompleted property exists (for backward compatibility)
            if (parsedSettings.Property(nameof(AppConfiguration.WizardCompleted), StringComparison.OrdinalIgnoreCase) == null)
            {
                Settings.WizardCompleted = false;
                settingsUpdated = true;
            }

            // Ensure SplashCountdownSeconds property exists (for backward compatibility)
            if (parsedSettings.Property(nameof(AppConfiguration.SplashCountdownSeconds), StringComparison.OrdinalIgnoreCase) == null)
            {
                Settings.SplashCountdownSeconds = 1;
                settingsUpdated = true;
            }

            // Ensure UseSpecificMonitor property exists (for backward compatibility)
            if (parsedSettings.Property(nameof(AppConfiguration.UseSpecificMonitor), StringComparison.OrdinalIgnoreCase) == null)
            {
                Settings.UseSpecificMonitor = false;
                Settings.PreferredMonitorIndex = 0;
                settingsUpdated = true;
            }

            // Force Robbel3D setup flag to true and request restart if it was explicitly false
            if (!Settings.ShowRobbel3DSetup)
            {
                Settings.ShowRobbel3DSetup = true;
                settingsUpdated = true;

                if (hasRobbel3DFlag && previousRobbel3DValue == false)
                {
                    RequiresRestart = true;
                }
            }

            if (settingsUpdated)
            {
                SaveSettings();
            }
        }

        private static AppConfiguration CreateDefaultSettings()
        {
            return new AppConfiguration
            {
                StartProfileOnStart = false,
                SkipUpdateConfirmation = false,
                IsBetaTester = false,
                NewSettingsMode = true,
                WizardCompleted = false,
                ShowRobbel3DSetup = true,
                SplashCountdownSeconds = 1,
                WindowWidth = 1000,
                WindowHeight = 800,
                WindowX = double.NaN,
                WindowY = double.NaN,
                NavColumnWidth = 250,
                TooltipColumnWidth = 250,
                UseSpecificMonitor = false,
                PreferredMonitorIndex = 0
            };
        }
    }
}
