using System.Collections.Generic;

namespace autodarts_desktop.model
{

    /// <summary>
    /// Provides default function for app-handling
    /// </summary>
    public interface IApp
    {

        public bool Install();

        public bool Run(Dictionary<string, string> runtimeArguments);

        public bool IsInstalled();

        public bool IsRunning();

        public void Close();

        public bool IsConfigurable();

        public bool IsInstallable();


    }
}
