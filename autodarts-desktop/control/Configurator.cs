using Newtonsoft.Json;
using System.IO;


namespace autodarts_desktop.control
{
    public class AppConfiguration
    {
        public bool StartProfileOnStart { get; set; }
    }



    public class Configurator
    {

        // ATTRIBUTES
        private readonly string ConfigFilePath;



        // METHODS

        public Configurator(string configFileName) 
        {
            ConfigFilePath = Path.Combine(Helper.GetAppBasePath(), "config.json");
        }


        public AppConfiguration LoadSettings()
        {
            if (!File.Exists(ConfigFilePath))
            {
                var defaultConfiguration = new AppConfiguration
                {
                    StartProfileOnStart = false
                };
                SaveSettings(defaultConfiguration);
                return defaultConfiguration;
            }

            var json = File.ReadAllText(ConfigFilePath);
            return JsonConvert.DeserializeObject<AppConfiguration>(json);
        }

        public void SaveSettings(AppConfiguration settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, json);
        }
    }
}
