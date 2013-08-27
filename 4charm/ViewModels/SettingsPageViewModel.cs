using _4charm.Models;
using Microsoft.Phone.Controls;

namespace _4charm.ViewModels
{
    class SettingsPageViewModel : ViewModelBase
    {
        public bool EnableManualRefresh
        {
            get { return CriticalSettingsManager.Current.EnableManualRefresh; }
            set { CriticalSettingsManager.Current.EnableManualRefresh = value; }
        }

        public bool ShowTripcodes
        {
            get { return CriticalSettingsManager.Current.ShowTripcodes; }
            set { CriticalSettingsManager.Current.ShowTripcodes = value; }
        }

        public bool ShowStickies
        {
            get { return CriticalSettingsManager.Current.ShowStickies; }
            set { CriticalSettingsManager.Current.ShowStickies = value; }
        }

        public bool EnableHTTPS
        {
            get { return CriticalSettingsManager.Current.EnableHTTPS; }
            set { CriticalSettingsManager.Current.EnableHTTPS = value; }
        }

        public SupportedPageOrientation LockOrientation
        {
            get { return CriticalSettingsManager.Current.LockOrientation; }
            set { CriticalSettingsManager.Current.LockOrientation = value; }
        }

        public SettingsPageViewModel()
        {
        }
    }
}
