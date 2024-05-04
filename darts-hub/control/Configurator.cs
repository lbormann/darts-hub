using Newtonsoft.Json;
using System.IO;


namespace darts_hub.control
{
    public class AppConfiguration
    {
        public bool StartProfileOnStart { get; set; }
        public bool SkipUpdateConfirmation { get; set; }
    }



    public class Configurator
    {
        // ATTRIBUTES
        private readonly string ConfigFilePath;
        public AppConfiguration Settings;



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


        private void LoadSettings()
        {
            if (!File.Exists(ConfigFilePath))
            {
                Settings = new AppConfiguration
                {
                    StartProfileOnStart = false,
                    SkipUpdateConfirmation = false
                };
                SaveSettings();
            }

            var json = File.ReadAllText(ConfigFilePath);
            Settings = JsonConvert.DeserializeObject<AppConfiguration>(json);
        }
    }
}
