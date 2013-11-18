using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace _4charm.Views
{
    public partial class ThreadsPage : PhoneApplicationPage
    {
        // We use this hack to notify the threads page to reload
        // after creating a new thread. The thread creation page
        // calls GoBack() and then navigated to the new post page,
        // but if the user hits back we want to show their thread
        // at the top of the list.
        internal static bool ForceReload = false;

        private ThreadsPageViewModel _viewModel;

        private ApplicationBarIconButton _refresh, _clear;
        private ApplicationBarMenuItem _orientLock;
        private int _lastUpdate = 0;
        private bool _watchlistNavigated = false;
        private bool _catalogNavigated = false;

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
                    if (TransitorySettingsManager.Current.Watchlist[i].Board.Name == _viewModel.Name)
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

            ApplicationBarMenuItem newThread = new ApplicationBarMenuItem(AppResources.ApplicationBar_NewThread);
            newThread.Click += (sender, e) =>
            {
                NavigationService.Navigate(new Uri(String.Format("/Views/NewThreadPage.xaml?board={0}", Uri.EscapeUriString(_viewModel.Name)), UriKind.Relative));
            };

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Buttons.Add(_refresh);
            ApplicationBar.MenuItems.Add(newThread);
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

            if (ForceReload)
            {
                ForceReload = false;
                _viewModel.Reload();
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

        private async void PivotChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            int index = ((Pivot)sender).SelectedIndex;
            if (index == _lastUpdate) return;

            _lastUpdate = index;
            ApplicationBar.Buttons.Clear();
            if (index == 0 || index == 2)
            {
                ApplicationBar.Buttons.Add(_refresh);
            }
            else
            {
                ApplicationBar.Buttons.Add(_clear);

                await Task.Delay(500);                

                if (!_watchlistNavigated && ((Pivot)sender).SelectedIndex == 1)
                {
                    _watchlistNavigated = true;
                    WatchlistLLS.Visibility = System.Windows.Visibility.Visible;
                    _viewModel.OnWatchlistNavigated();
                }
            }

            if (index == 2 && !_catalogNavigated)
            {
                _catalogNavigated = true;
                CatalogLLS.Visibility = System.Windows.Visibility.Visible;
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

                if (index == 14)
                {
                    _viewModel.FinishInsertingPosts();
                }
            }
        }

        private void ImageThreadRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                (e.Container.Content as ThreadViewModel).LoadImage(208);

                ThreadViewModel t = e.Container.Content as ThreadViewModel;
                int index = _viewModel.ImageThreads.IndexOf(t);
                if (_viewModel.ImageThreads.Count < 10)
                {
                    ((VisualTreeHelper.GetChild(e.Container, 0) as Grid).Resources["FadeInStoryboard"] as Storyboard).Begin();
                }
                if (index == 14)
                {
                    _viewModel.FinishInsertingPosts();
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