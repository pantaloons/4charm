using _4charm.Controls;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace _4charm.Views
{
    public partial class ThreadsPage : OrientLockablePage
    {
        private ThreadsPageViewModel _viewModel;

        private ApplicationBarIconButton _refresh, _clear;

        public ThreadsPage()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new ThreadsPageViewModel();
            DataContext = _viewModel;
        }

        private void InitializeApplicationBar()
        {
            _refresh = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.refresh.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Refresh };
            _refresh.Click += (sender, e) => _viewModel.ReloadThreads();

            _clear = new ApplicationBarIconButton(new Uri("/Assets/Appbar/appbar.delete.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Clear };
            _clear.Click += (sender, e) => _viewModel.ClearWatchlist();

            ApplicationBarMenuItem newThread = new ApplicationBarMenuItem(AppResources.ApplicationBar_NewThread);
            newThread.Click += (sender, e) => NavigationService.Navigate(new Uri(String.Format("/Views/NewThreadPage.xaml?board={0}", Uri.EscapeUriString(_viewModel.Name)), UriKind.Relative));

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Buttons.Add(_refresh);
            ApplicationBar.MenuItems.Add(newThread);
            ApplicationBar.MenuItems.Add(_orientLock);
        }

        private void PivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems[0] == null)
            {
                return;
            }

            int index = ((Pivot)sender).SelectedIndex;

            ApplicationBar.Buttons.Clear();
            if (index == 0 || index == 2)
            {
                ApplicationBar.Buttons.Add(_refresh);
            }
            else
            {
                ApplicationBar.Buttons.Add(_clear);
            }
        }
    }
}