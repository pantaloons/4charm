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

        private ApplicationBarIconButton _refresh, _search, _clear;

        public ThreadsPage()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new ThreadsPageViewModel();
            DataContext = _viewModel;

            _viewModel.SearchStateChanged += SearchStateChanged;
            SearchBox.GotFocus += SearchBox_GotFocus;
            SearchBox.LostFocus += SearchBox_LostFocus;
        }

        private void InitializeApplicationBar()
        {
            _refresh = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.refresh.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Refresh };
            _refresh.Click += (sender, e) => _viewModel.ReloadThreads();

            _search = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.search.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Search };
            _search.Click += (sender, e) => _viewModel.ShowSearchBox();

            _clear = new ApplicationBarIconButton(new Uri("/Assets/Appbar/appbar.delete.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Clear };
            _clear.Click += (sender, e) => _viewModel.ClearWatchlist();

            ApplicationBarMenuItem newThread = new ApplicationBarMenuItem(AppResources.ApplicationBar_NewThread);
            newThread.Click += (sender, e) => _viewModel.CreateNewThread();

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Buttons.Add(_refresh);
            ApplicationBar.MenuItems.Add(newThread);
            ApplicationBar.MenuItems.Add(_orientLock);
        }

        private void SearchStateChanged(object sender, EventArgs e)
        {
            if (_viewModel.IsSearching)
            {
                SearchBox.Focus();
            }

            if (ThreadsLLS.ItemsSource.Count > 0)
            {
                ThreadsLLS.UpdateLayout();
                ThreadsLLS.ScrollTo(ThreadsLLS.ItemsSource[0]);
            }
            if (WatchlistLLS.ItemsSource.Count > 0)
            {
                WatchlistLLS.UpdateLayout();
                WatchlistLLS.ScrollTo(WatchlistLLS.ItemsSource[0]);
            }
            if (CatalogLLS.ItemsSource.Count > 0)
            {
                CatalogLLS.UpdateLayout();
                CatalogLLS.ScrollTo(CatalogLLS.ItemsSource[0]);
            }
        }

        private void SearchBox_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplicationBar.IsVisible = false;
        }

        private void SearchBox_LostFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            ApplicationBar.IsVisible = true;
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

                if (index == 2)
                {
                    ApplicationBar.Buttons.Add(_search);
                }
            }
            else
            {
                ApplicationBar.Buttons.Add(_clear);
            }
        }
    }
}