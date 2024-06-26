﻿using System.Collections.Generic;

namespace darts_hub.model
{

    /// <summary>
    /// Provides default function for app-handling
    /// </summary>
    public interface IApp
    {

        public bool Install();

        public bool Run(Dictionary<string, string> runtimeArguments);

        public bool ReRun(Dictionary<string, string> runtimeArguments);

        public bool IsInstalled();

        public bool IsRunning();

        public bool IsConfigurationChanged();

        public void Close();

        public bool IsConfigurable();

        public bool IsInstallable();


    }
}
