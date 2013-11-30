using _4charm.Controls;
using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Windows.Navigation;

namespace _4charm.Views
{
    public partial class ImageViewer : OrientLockablePage
    {
        private ImageViewerPageViewModel _viewModel;
        
        public ImageViewer()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new ImageViewerPageViewModel();
            DataContext = _viewModel;
        }

        private void InitializeApplicationBar()
        {
            ApplicationBarIconButton saveImage = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.download.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Save };
            saveImage.Click += async (sender, e) => await _viewModel.Save();

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Opacity = 0.0;
            ApplicationBar.Buttons.Add(saveImage);
            ApplicationBar.MenuItems.Add(_orientLock);
            ApplicationBar.StateChanged += AppplicationBar_Opened;
        }

        private void AppplicationBar_Opened(object sender, ApplicationBarStateChangedEventArgs e)
        {
            ApplicationBar.Opacity = e.IsMenuVisible ? 0.99 : 0.0;
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