using _4charm.Models;
using _4charm.Resources;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;

namespace _4charm.Controls
{
    public abstract class OrientLockablePage : BoundPage
    {
        protected ApplicationBarMenuItem _orientLock;

        public OrientLockablePage()
        {
            _orientLock = new ApplicationBarMenuItem(GetOrientationLockText());
            _orientLock.Click += (sender, e) =>
            {
                if (CriticalSettingsManager.Current.LockOrientation == SupportedPageOrientation.PortraitOrLandscape)
                {
                    bool isPortrait =
                        Orientation == PageOrientation.Portrait || Orientation == PageOrientation.PortraitDown ||
                        Orientation == PageOrientation.PortraitUp;

                    CriticalSettingsManager.Current.LockOrientation = isPortrait ? SupportedPageOrientation.Portrait : SupportedPageOrientation.Landscape;
                }
                else
                {
                    CriticalSettingsManager.Current.LockOrientation = SupportedPageOrientation.PortraitOrLandscape;
                }

                SupportedOrientations = CriticalSettingsManager.Current.LockOrientation;
                _orientLock.Text = GetOrientationLockText();
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SupportedOrientations = CriticalSettingsManager.Current.LockOrientation;
        }

        private string GetOrientationLockText()
        {
            if (CriticalSettingsManager.Current.LockOrientation == SupportedPageOrientation.PortraitOrLandscape)
            {
                return AppResources.ApplicationBar_LockOrientation;
            }
            else
            {
                return AppResources.ApplicationBar_UnlockOrientation;
            }
        }
    }
}
