using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace _4charm.Views
{
    public partial class ThreadsPage : PhoneApplicationPage
    {
        private ThreadsPageViewModel _viewModel;

        private ApplicationBarIconButton _refresh, _clear;
        private ApplicationBarMenuItem _orientLock;
        private int _lastUpdate = 0;
        private bool _watchlistNavigated = false;

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
            _refresh.Click += (sender, e) =>
            {
                _viewModel.Reload();
            };
            _clear = new ApplicationBarIconButton(new Uri("/Assets/Appbar/appbar.delete.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Clear };
            _clear.Click += (sender, e) =>
            {
                for (int i = 0; i < TransitorySettingsManager.Current.Watchlist.Count; i++)
                {
                    if (TransitorySettingsManager.Current.Watchlist[i].BoardName == _viewModel.Name)
                    {
                        TransitorySettingsManager.Current.Watchlist.RemoveAt(i);
                        i--;
                    }
                }
            };

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
            ApplicationBar.Buttons.Add(_refresh);
            ApplicationBar.MenuItems.Add(_orientLock);
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

        private bool _initialized;
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!_initialized)
            {
                string boardName = NavigationContext.QueryString["board"];
                _viewModel.OnNavigatedTo(boardName);

                _initialized = true;
            }

            OrientationLockChanged();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _viewModel.OnNavigatedFrom(e);
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            _viewModel.OnRemovedFromJournal();
        }

        private void PivotChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int index = ((Pivot)sender).SelectedIndex;
            if (index == _lastUpdate) return;

            _lastUpdate = index;
            ApplicationBar.Buttons.Clear();
            if (index == 0)
            {
                ApplicationBar.Buttons.Add(_refresh);
            }
            else
            {
                ApplicationBar.Buttons.Add(_clear);

                if (!_watchlistNavigated)
                {
                    _watchlistNavigated = true;

                    WatchlistLLS.Visibility = System.Windows.Visibility.Visible;
                    _viewModel.OnWatchlistNavigated();
                }
            }
        }

        private void ThreadRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                (e.Container.Content as ThreadViewModel).LoadImage();

                ThreadViewModel t = e.Container.Content as ThreadViewModel;
                int index = _viewModel.Threads.IndexOf(t);
                if (_viewModel.Threads.Count < 10)
                {
                    ((VisualTreeHelper.GetChild(e.Container, 0) as Grid).Resources["FadeInStoryboard"] as Storyboard).Begin();
                }
            }
        }

        private void ThreadUnrealized(object sender, ItemRealizationEventArgs e)
        {
            //if (e.ItemKind == LongListSelectorItemKind.Item)
            //{
            //    (e.Container.Content as ThreadViewModel).UnloadImage();
            //}
        }
    }
}