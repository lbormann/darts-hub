using System.ComponentModel;
using darts_hub.control;

namespace darts_hub.ViewModels
{
    public class UpdaterViewModel : INotifyPropertyChanged
    {
        private bool _isBetaTester;

        public bool IsBetaTester
        {
            get => _isBetaTester;
            set
            {
                if (_isBetaTester != value)
                {
                    _isBetaTester = value;
                    OnPropertyChanged(nameof(IsBetaTester));
                    Updater.IsBetaTester = value;
                    SaveBetaTesterStatus(value);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SaveBetaTesterStatus(bool isBetaTester)
        {
            var configurator = new Configurator("config.json");
            configurator.Settings.IsBetaTester = isBetaTester;
            configurator.SaveSettings();
        }
    }
}