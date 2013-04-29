using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace _4charm.Views
{
    public partial class PostsPage : PhoneApplicationPage
    {
        private PostsPageViewModel _viewModel;

        private ApplicationBarIconButton _watch;
        private ApplicationBarMenuItem _orientLock;

        private Thread _thread;
        private enum BackState
        {
            None,
            Quotes,
            Reply
        };
        BackState _backState;

        public PostsPage()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new PostsPageViewModel();
            DataContext = _viewModel;
        }

        private void InitializeApplicationBar()
        {
            _watch = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.eye.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Watch };
            _watch.Click += (sender, e) =>
            {
                ThreadViewModel tvm = SettingsManager.Current.Watchlist.FirstOrDefault(x => x.BoardName == _thread.Board.Name && x.Number == _thread.Number);
                if (tvm != null) SettingsManager.Current.Watchlist.Remove(tvm);
                else SettingsManager.Current.Watchlist.Add(new ThreadViewModel(_thread));

                UpdateWatchButton();
            };

            ApplicationBarMenuItem bottom = new ApplicationBarMenuItem(AppResources.ApplicationBar_ScrollToBottom);
            bottom.Click += (sender, e) =>
            {
                if (MainPivot.SelectedIndex == 0)
                {
                    TextLLS.ScrollTo(_viewModel.AllPosts.Last());
                }
                else
                {
                    ImageLLS.ScrollTo(_viewModel.ImagePosts.Last());
                }
            };

            _orientLock = new ApplicationBarMenuItem(AppResources.ApplicationBar_LockOrientation);
            _orientLock.Click += (sender, e) =>
            {
                if (SettingsManager.Current.LockOrientation == SupportedPageOrientation.PortraitOrLandscape)
                {
                    bool isPortrait =
                        Orientation == PageOrientation.Portrait || Orientation == PageOrientation.PortraitDown ||
                        Orientation == PageOrientation.PortraitUp;

                    SettingsManager.Current.LockOrientation = isPortrait ? SupportedPageOrientation.Portrait : SupportedPageOrientation.Landscape;
                }
                else
                {
                    SettingsManager.Current.LockOrientation = SupportedPageOrientation.PortraitOrLandscape;
                }
                OrientationLockChanged();
            };

            ApplicationBar = new ApplicationBar();
            ApplicationBar.Buttons.Add(_watch);
            ApplicationBar.MenuItems.Add(bottom);
            ApplicationBar.MenuItems.Add(_orientLock);
        }

        private void OrientationLockChanged()
        {
            this.SupportedOrientations = SettingsManager.Current.LockOrientation;
            if (this.SupportedOrientations == SupportedPageOrientation.PortraitOrLandscape)
            {
                _orientLock.Text = AppResources.ApplicationBar_LockOrientation;
            }
            else
            {
                _orientLock.Text = AppResources.ApplicationBar_UnlockOrientation;
            }
        }

        private void UpdateWatchButton()
        {
            bool watchlisted = SettingsManager.Current.Watchlist.Count(x => x.BoardName == _thread.Board.Name && x.Number == _thread.Number) > 0;
            if (watchlisted)
            {
                _watch.IconUri = new Uri("Assets/Appbar/appbar.eye.check.png", UriKind.Relative);
                _watch.Text = AppResources.ApplicationBar_Unwatch;
            }
            else
            {
                _watch.IconUri = new Uri("Assets/Appbar/appbar.eye.png", UriKind.Relative);
                _watch.Text = AppResources.ApplicationBar_Watch;
            }

            //if (new Random().Next() % 2 == 1) ExpandStoryboard.Begin();
            //else CollapseStoryboard.Begin();
        }

        private bool _initialized;
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (!_initialized)
            {
                string boardName = NavigationContext.QueryString["board"];
                ulong threadID = ulong.Parse(NavigationContext.QueryString["thread"]);

                _thread = ThreadCache.Current.EnforceBoard(boardName).EnforceThread(threadID);

                bool doScroll = false;
                ulong scrollTo = 0;
                if (NavigationContext.QueryString.ContainsKey("post"))
                {
                    doScroll = true;
                    scrollTo = ulong.Parse(NavigationContext.QueryString["post"]);
                }

                _viewModel.OnNavigatedTo(boardName, threadID, () =>
                {
                    if (doScroll)
                    {
                        PostViewModel pvm = _viewModel.AllPosts.First(x => x.Number == scrollTo);
                        if (pvm != null)
                        {
                            MainPivot.SelectedIndex = 0;
                            TextLLS.UpdateLayout();
                            TextLLS.ScrollTo(pvm);
                        }
                    }
                }, FilterApplied);

                ThreadViewModel tvm = SettingsManager.Current.History.FirstOrDefault(x => x.BoardName == _thread.Board.Name && x.Number == _thread.Number);
                if (tvm != null) SettingsManager.Current.History.Remove(tvm);
                SettingsManager.Current.History.Insert(0, new ThreadViewModel(_thread));

                UpdateWatchButton();

                _initialized = true;
            }

            OrientationLockChanged();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            _viewModel.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (MainPivot.SelectedIndex == 0)
            {
                switch (_backState)
                {
                    case BackState.Quotes:
                        (TextLLS.RenderTransform as CompositeTransform).TranslateY = 224;
                        TextLLS.Margin = new Thickness(12, 0, 0, 0);
                        CollapseStoryboard.Begin();
                        _backState = BackState.None;
                        e.Cancel = true;
                        break;
                    case BackState.Reply:
                        break;
                    case BackState.None:
                        break;
                }
            }
        }

        private void FilterApplied()
        {
            if (_backState != BackState.Quotes)
            {
                TextLLS.Margin = new Thickness(12, 0, 0, 0);
                ExpandStoryboard.Begin();
                _backState = BackState.Quotes;
            }
        }

        private void LLS_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                PostViewModel p = e.Container.DataContext as PostViewModel;
                p.LoadImage();
            }
        }

        private void SelectionLLS_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            LLS_ItemRealized(sender, e);
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                ((VisualTreeHelper.GetChild(e.Container, 0) as Grid).Resources["FlashStoryboard"] as Storyboard).Begin();
            }
        }

        private void LLS_ItemUnrealized(object sender, ItemRealizationEventArgs e)
        {
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                PostViewModel p = e.Container.DataContext as PostViewModel;
                p.UnloadImage();
            }
        }

        private void MainPivotSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainPivot.SelectedIndex == 1)
            {
                TextLLS.Visibility = Visibility.Collapsed;
                ImageLLS.Visibility = Visibility.Visible;
            }
            else
            {
                TextLLS.Visibility = Visibility.Visible;
                ImageLLS.Visibility = Visibility.Collapsed;
            }
        }

        private void ExpandStoryboardCompleted(object sender, EventArgs e)
        {
            (TextLLS.RenderTransform as CompositeTransform).TranslateY = 0;
            TextLLS.Margin = new Thickness(12, 224, 0, 0);
        }
    }
}