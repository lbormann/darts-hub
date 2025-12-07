using Newtonsoft.Json;
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
    }



    public class Configurator
    {
        // ATTRIBUTES
        private readonly string ConfigFilePath;
        public AppConfiguration Settings { get; private set; }



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
                Settings = new AppConfiguration
                {
                    StartProfileOnStart = false,
                    SkipUpdateConfirmation = false,
                    IsBetaTester = false,
                    NewSettingsMode = true, // Changed from false to true - new installations get enhanced settings mode by default
                    WizardCompleted = false
                };
                SaveSettings();
            }

            var json = File.ReadAllText(ConfigFilePath);
            Settings = JsonConvert.DeserializeObject<AppConfiguration>(json);
            
            // Ensure NewSettingsMode property exists (for backward compatibility)
            if (Settings.NewSettingsMode == null)
            {
                Settings.NewSettingsMode = false;
                SaveSettings();
            }
            
            // Ensure WizardCompleted property exists (for backward compatibility)
            if (Settings.WizardCompleted == null)
            {
                Settings.WizardCompleted = false;
                SaveSettings();
            }
        }
    }
}
