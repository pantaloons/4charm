using _4charm.Models;
using Microsoft.Phone.Controls;

namespace _4charm.ViewModels
{
    class SettingsPageViewModel : ViewModelBase
    {
        public bool ShowTripcodes
        {
            get { return SettingsManager.Current.ShowTripcodes; }
            set { SettingsManager.Current.ShowTripcodes = value; }
        }

        public bool ShowStickies
        {
            get { return SettingsManager.Current.ShowStickies; }
            set { SettingsManager.Current.ShowStickies = value; }
        }

        public bool EnableHTTPS
        {
            get { return SettingsManager.Current.EnableHTTPS; }
            set { SettingsManager.Current.EnableHTTPS = value; }
        }

        public SupportedPageOrientation LockOrientation
        {
            get { return SettingsManager.Current.LockOrientation; }
            set { SettingsManager.Current.LockOrientation = value; }
        }

        public SettingsPageViewModel()
        {
        }
    }
}
