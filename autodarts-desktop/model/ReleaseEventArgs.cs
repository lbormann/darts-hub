using System;


namespace autodarts_desktop.model
{
    /// <summary>
    /// Event-args for a release.
    /// </summary>
    public class ReleaseEventArgs : EventArgs
    {
        private readonly string _version;
        private readonly string _message;

        public ReleaseEventArgs(string version, string message)
        {
            _version = version;
            _message = message;
        }

        public string Version
        {
            get { return _version; }
        }
        public string Message
        {
            get { return _message; }
        }
    }
}
