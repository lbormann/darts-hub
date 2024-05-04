using System;


namespace darts_hub.model
{
    /// <summary>
    /// An exception for broken configuration files.
    /// </summary>
    public class ConfigurationException : Exception
    {
        // ATTRIBUTES

        private readonly string _file;
        private readonly string _message;


        // METHODS

        public ConfigurationException(string file, string message)
        {
            _file = file;
            _message = message;
        }


        public string File
        {
            get { return _file; }
        }
        public override string Message
        {
            get { return _message; }
        }

    }
}
