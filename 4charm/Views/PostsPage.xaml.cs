using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace _4charm.Views
{
    public partial class PostsPage : PhoneApplicationPage
    {
        private PostsPageViewModel _viewModel;

        private ApplicationBarIconButton _refresh, _watch, _reply, _send;
        private ApplicationBarMenuItem _orientLock;
        private System.Threading.Timer _refreshTimer;

        private Task _threadLoadTask;

        private bool _scrollTop;

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
            _refresh = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.refresh.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Refresh };
            _refresh.Click += async (sender, e) =>
            {
                await _viewModel.Update();
            };

            _watch = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.eye.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Watch };
            _watch.Click += (sender, e) =>
            {
                Thread thread = TransitorySettingsManager.Current.Watchlist.FirstOrDefault(x => x.Board.Name == _thread.Board.Name && x.Number == _thread.Number);
                if (thread != null)
                {
                    TransitorySettingsManager.Current.Watchlist.Remove(thread);
                }
                else
                {
                    TransitorySettingsManager.Current.Watchlist.Add(_thread);
                }

                UpdateWatchButton();
            };

            _reply = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.reply.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Reply };
            _reply.Click += (sender, e) =>
            {
                TransitionToState(BackState.Reply);
            };

            _send = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.send.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Send };
            _send.Click += async (sender, e) =>
            {
                // Focus and then dispatch to ensure the two-way binding updates
                if (FocusManager.GetFocusedElement() is TextBox)
                {
                    var binding = (FocusManager.GetFocusedElement() as TextBox).GetBindingExpression(TextBox.TextProperty);
                    binding.UpdateSource();
                }

                HighlightCaptchaStoryboard.Stop();
                HighlightCommentStoryboard.Stop();
                Focus();

                MainPivot.IsLocked = true;
                _send.IsEnabled = false;
                BeginPostingStoryboard.Begin();
                ReplyScroller.IsHitTestVisible = false;
                SubmitResult result = await _viewModel.ReplyViewModel.Submit();
                MainPivot.IsLocked = false;
                _send.IsEnabled = true;
                BeginPostingStoryboard.Stop();
                ReplyScroller.IsHitTestVisible = true;

                switch(result.ResultType)
                {
                    case SubmitResultType.Success:
                        TransitionToState(BackState.None);
                        ReplyScroller.ScrollToVerticalOffset(0);
                        await _viewModel.Update();
                        TextLLS.ScrollTo(_viewModel.AllPosts.Last());
                        break;
                    case SubmitResultType.EmptyCaptchaError:
                    case SubmitResultType.WrongCatpchaError:
                        HighlightCaptchaStoryboard.Begin();
                        CaptchaTextBox.Focus();
                        ReplyScroller.ScrollToVerticalOffset(InnerReplyGrid.RowDefinitions[0].ActualHeight - 10);
                        CaptchaTextBox.Text = "";
                        if(result.ResultType == SubmitResultType.WrongCatpchaError) _viewModel.ReplyViewModel.ReloadCaptcha.Execute(null);
                        break;
                    case SubmitResultType.EmptyCommentError:
                        HighlightCommentStoryboard.Begin();
                        CommentTextBox.Focus();
                        ReplyScroller.ScrollToVerticalOffset(0);
                        break;
                    case SubmitResultType.KnownError:
                        MessageBox.Show(result.ErrorMessage);
                        break;
                    case SubmitResultType.NoImageError:
                    case SubmitResultType.UnknownError:
                        MessageBox.Show("Unknown error encountered submitting post.");
                        break;
                }
            };

            ApplicationBarMenuItem bottom = new ApplicationBarMenuItem(AppResources.ApplicationBar_ScrollToBottom);
            bottom.Click += (sender, e) =>
            {
                _viewModel.ForceFastLoading = true;
                _threadLoadTask.ContinueWith(t =>
                {
                    if (MainPivot.SelectedIndex == 0)
                    {
                        if (_viewModel.AllPosts.Count > 0)
                        {
                            if (_scrollTop) TextLLS.ScrollTo(_viewModel.AllPosts.First());
                            else TextLLS.ScrollTo(_viewModel.AllPosts.Last());
                        }
                    }
                    else
                    {
                        if (_viewModel.ImagePosts.Count > 0)
                        {
                            if (_scrollTop) ImageLLS.ScrollTo(_viewModel.ImagePosts.First());
                            else ImageLLS.ScrollTo(_viewModel.ImagePosts.Last());
                        }
                    }
                    //_scrollTop = !_scrollTop;
                    //bottom.Text = _scrollTop ? AppResources.ApplicationBar_ScrollToTop : AppResources.ApplicationBar_ScrollToBottom;
                }, TaskScheduler.FromCurrentSynchronizationContext());
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
            if (CriticalSettingsManager.Current.EnableManualRefresh)
            {
                ApplicationBar.Buttons.Add(_refresh);
            }
            ApplicationBar.Buttons.Add(_reply);
            ApplicationBar.Buttons.Add(_watch);
            ApplicationBar.MenuItems.Add(bottom);
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

        private void UpdateWatchButton()
        {
            bool watchlisted = TransitorySettingsManager.Current.Watchlist.Count(x => x.Board.Name == _thread.Board.Name && x.Number == _thread.Number) > 0;
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

                _threadLoadTask = _viewModel.OnNavigatedTo(boardName, threadID, doScroll, () =>
                {
                    if (doScroll)
                    {
                        PostViewModel pvm = _viewModel.AllPosts.FirstOrDefault(x => x.Number == scrollTo);
                        if (pvm != null)
                        {
                            MainPivot.SelectedIndex = 0;
                            TextLLS.UpdateLayout();
                            TextLLS.ScrollTo(pvm);
                        }
                    }

                    Thread thread = TransitorySettingsManager.Current.History.FirstOrDefault(x => x.Board.Name == _thread.Board.Name && x.Number == _thread.Number);
                    if (thread != null)
                    {
                        TransitorySettingsManager.Current.History.Remove(thread);
                    }
                    TransitorySettingsManager.Current.History.Insert(0, _thread);
                }, FilterApplied);

                UpdateWatchButton();

                _initialized = true;
            }

            if (!CriticalSettingsManager.Current.EnableManualRefresh)
            {
                _refreshTimer = new System.Threading.Timer(state =>
                {
                    Dispatcher.BeginInvoke(async () => await _viewModel.Update());
                }, null, 30 * 1000, 30 * 1000);
            }

            OrientationLockChanged();
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (_refreshTimer != null) _refreshTimer.Dispose();
            _refreshTimer = null;

            _viewModel.OnNavigatedFrom(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            switch (_backState)
            {
                case BackState.Quotes:
                case BackState.Reply:
                    TransitionToState(BackState.None);
                    e.Cancel = true;
                    break;
                case BackState.None:
                    break;
            }

            UpdateApplicationBar();
        }

        private void TransitionToState(BackState desired)
        {
            if (_viewModel.ReplyViewModel.IsPosting) return;

            if (_backState == BackState.Reply)
            {
                _viewModel.ReplyViewModel.UnloadImage();
            }

            switch (_backState)
            {
                case BackState.None:
                    TransitionFromNone(desired);
                    break;
                case BackState.Quotes:
                    TransitionFromQuotes(desired);
                    break;
                case BackState.Reply:
                    TransitionFromReply(desired);
                    break;
            }

            if (desired == BackState.Reply)
            {
                ReplyScroller.ScrollToVerticalOffset(0);
                _viewModel.ReplyViewModel.Load();
            }
        }

        private void TransitionFromNone(BackState desired)
        {
            switch (desired)
            {
                case BackState.None:
                    break;
                case BackState.Quotes:
                    TextLLS.Margin = new Thickness(12, 0, 0, 0);
                    ExpandSelectionStoryboard.Begin();
                    _backState = BackState.Quotes;
                    break;
                case BackState.Reply:
                    TextLLS.Margin = new Thickness(12, 0, 0, 0);
                    ExpandReplyStoryboard.Begin();
                    _backState = BackState.Reply;
                    UpdateApplicationBar();
                    break;
            }
        }

        private void TransitionFromQuotes(BackState desired)
        {
            switch (desired)
            {
                case BackState.None:
                    ExpandSelectionStoryboard.Stop();
                    (TextLLS.RenderTransform as CompositeTransform).TranslateY = 224;
                    TextLLS.Margin = new Thickness(12, 0, 0, 0);
                    CollapseSelectionStoryboard.Begin();
                    _backState = BackState.None;
                    break;
                case BackState.Quotes:
                    break;
                case BackState.Reply:
                    HideSelectionStoryboard.Begin();
                    ShowReplyStoryboard.Begin();
                    _backState = BackState.Reply;
                    UpdateApplicationBar();
                    _viewModel.ReplyViewModel.LoadImage();
                    break;
            }
        }

        private void TransitionFromReply(BackState desired)
        {
            switch (desired)
            {
                case BackState.None:
                    ExpandReplyStoryboard.Stop();
                    (TextLLS.RenderTransform as CompositeTransform).TranslateY = 224;
                    TextLLS.Margin = new Thickness(12, 0, 0, 0);
                    CollapseReplyStoryboard.Begin();
                    _backState = BackState.None;
                    UpdateApplicationBar();
                    break;
                case BackState.Quotes:
                    HideReplyStoryboard.Begin();
                    ShowSelectionStoryboard.Begin();
                    _backState = BackState.Quotes;
                    UpdateApplicationBar();
                    break;
                case BackState.Reply:
                    break;
            }
        }

        private void UpdateApplicationBar()
        {
            ApplicationBar.Buttons.Clear();
            switch (_backState)
            {
                case BackState.None:
                case BackState.Quotes:
                    if (CriticalSettingsManager.Current.EnableManualRefresh)
                    {
                        ApplicationBar.Buttons.Add(_refresh);
                    }
                    ApplicationBar.Buttons.Add(_reply);
                    ApplicationBar.Buttons.Add(_watch);
                    break;
                case BackState.Reply:
                    if (CriticalSettingsManager.Current.EnableManualRefresh)
                    {
                        ApplicationBar.Buttons.Add(_refresh);
                    }
                    ApplicationBar.Buttons.Add(_send);
                    break;
            }
        }

        private void FilterApplied()
        {
            TransitionToState(BackState.Quotes);
        }

        private void RootGridTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_backState != BackState.Reply || _viewModel.ReplyViewModel.IsPosting) return;

            CommentTextBox.Text += ">>" + ((sender as FrameworkElement).DataContext as PostViewModel).Number + "\n";
            CommentTextBox.SelectionStart = CommentTextBox.Text.Length;
        }

        private void RootGridDoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_backState == BackState.Reply || _viewModel.ReplyViewModel.IsPosting)
            {
                e.Handled = true;
                return;
            }

            CommentTextBox.Text = ">>" + ((sender as FrameworkElement).DataContext as PostViewModel).Number + "\n";
            CommentTextBox.SelectionStart = CommentTextBox.Text.Length;

            TransitionToState(BackState.Reply);
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
            //if (e.ItemKind == LongListSelectorItemKind.Item)
            //{
            //    PostViewModel p = e.Container.DataContext as PostViewModel;
            //    p.UnloadImage();
            //}
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

        private void ContextMenuOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var owner = (FrameworkElement)menu.Owner;
            if (owner.DataContext != menu.DataContext) menu.DataContext = owner.DataContext;
        }
    }
}