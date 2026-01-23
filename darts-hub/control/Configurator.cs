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
        public bool IsBetaTester { get; set; } // Neue Eigenschaft für den Betatester-Status
        public bool NewSettingsMode { get; set; } // Neue Eigenschaft für den neuen Settings-Modus
        public bool WizardCompleted { get; set; } // Neue Eigenschaft für den Wizard-Status
        public bool ShowRobbel3DSetup { get; set; } // Neue Eigenschaft für die Robbel3D Setup Sichtbarkeit
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
                NewSettingsMode = true, // Changed from false to true - new installations get enhanced settings mode by default
                WizardCompleted = false,
                ShowRobbel3DSetup = true // Always enable Robbel3D setup by default
            };
        }
    }
}
