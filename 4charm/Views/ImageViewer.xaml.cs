using _4charm.Controls;
using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Windows;
using System.Windows.Navigation;

namespace _4charm.Views
{
    public partial class ImageViewer : BoundPage
    {
        private ImageViewerPageViewModel _viewModel;

        private ApplicationBarIconButton _saveImage;
        private ApplicationBarMenuItem _orientLock;
        
        public ImageViewer()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new ImageViewerPageViewModel();
            DataContext = _viewModel;
        }

        private void InitializeApplicationBar()
        {
            _saveImage = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.download.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Save };
            _saveImage.Click += async (sender, e) => await _viewModel.Save();

            _orientLock = new ApplicationBarMenuItem(AppResources.ApplicationBar_LockOrientation);
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
                OrientationLockChanged();
            };

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.0;
            ApplicationBar.Buttons.Add(_saveImage);
            ApplicationBar.MenuItems.Add(_orientLock);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            OrientationLockChanged();
            ApplicationBar.StateChanged += AppplicationBar_Opened;
        }

        private void AppplicationBar_Opened(object sender, ApplicationBarStateChangedEventArgs e)
        {
            ApplicationBar.Opacity = e.IsMenuVisible ? 0.99 : 0.0;
        }

        private void OrientationLockChanged()
        {
            this.SupportedOrientations = CriticalSettingsManager.Current.LockOrientation;
            if (this.SupportedOrientations == SupportedPageOrientation.PortraitOrLandscape)
            {
                _orientLock.Text = AppResources.ApplicationBar_LockOrientation;
            }
            else
            {
                _orientLock.Text = AppResources.ApplicationBar_UnlockOrientation;
            }
        }

        private void MediaViewer_ItemZoomed(object sender, EventArgs e)
        {
            ApplicationBar.IsVisible = false;
        }

        private void MediaViewer_ItemUnzoomed(object sender, EventArgs e)
        {
            ApplicationBar.IsVisible = true;
        }
    }
}