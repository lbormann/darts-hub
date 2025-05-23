﻿using Newtonsoft.Json;
using System.IO;


namespace darts_hub.control
{
    public class AppConfiguration
    {
        public bool StartProfileOnStart { get; set; }
        public bool SkipUpdateConfirmation { get; set; }
        public bool IsBetaTester { get; set; } // Neue Eigenschaft für den Betatester-Status
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


        private void LoadSettings()
        {
            if (!File.Exists(ConfigFilePath))
            {
                Settings = new AppConfiguration
                {
                    StartProfileOnStart = false,
                    SkipUpdateConfirmation = false,
                    IsBetaTester = false
                };
                SaveSettings();
            }

            var json = File.ReadAllText(ConfigFilePath);
            Settings = JsonConvert.DeserializeObject<AppConfiguration>(json);
        }
    }
}
