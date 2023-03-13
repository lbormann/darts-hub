using System;


namespace autodarts_desktop.model
{
    /// <summary>
    /// Event-Args for an app
    /// </summary>
    public class AppEventArgs : EventArgs
    {
        private readonly AppBase _app;
        private readonly string _message;

        public AppEventArgs(AppBase app, string message)
        {
            _app = app;
            _message = message;
        }

        public AppBase App
        {
            get { return _app; }
        }
        public string Message
        {
            get { return _message; }
        }

    }
}
