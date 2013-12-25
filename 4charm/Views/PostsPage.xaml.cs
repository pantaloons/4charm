using _4charm.Controls;
using _4charm.Models;
using _4charm.Resources;
using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace _4charm.Views
{
    public partial class PostsPage : OrientLockablePage
    {
        private bool _isFixingCaptchaFocus;
        private PostsPageViewModel _viewModel;

        private ApplicationBarIconButton _refresh, _watch, _reply, _send, _edit;

        public PostsPage()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new PostsPageViewModel();
            DataContext = _viewModel;

            _viewModel.ViewStateChanged += ViewStateChanged;
            _viewModel.ScrollTargetLoaded += ScrollTargetLoaded;
            _viewModel.CaptchaFocused += CaptchaFocused;

            OrientationChanged += PostsPage_OrientationChanged;
        }

        private void InitializeApplicationBar()
        {
            _refresh = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.refresh.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Refresh };
            _refresh.Click += async (sender, e) => await _viewModel.Update();

            _watch = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.eye.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Watch };
            _watch.Click += (sender, e) =>
            {
                _viewModel.ToggleWatchlisted();
                UpdateWatchButton();
            };

            _reply = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.reply.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Reply };
            _reply.Click += (sender, e) => _viewModel.OpenReplyRegion();

            _send = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.send.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Send };
            _send.Click += async (sender, e) =>
            {
                _send.IsEnabled = false;
                Focus();
                ReplyPageViewModelBase.SubmitResultType result = await _viewModel.Submit();
                _send.IsEnabled = true;

                switch(result)
                {
                    case ReplyPageViewModelBase.SubmitResultType.Success:
                        ScrollToBottom();
                        break;
                    case ReplyPageViewModelBase.SubmitResultType.EmptyCaptchaError:
                    case ReplyPageViewModelBase.SubmitResultType.WrongCatpchaError:
                        Focus();
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            CaptchaTextBox.Focus();
                        });
                        break;
                    case ReplyPageViewModelBase.SubmitResultType.EmptyCommentError:
                        CommentBox.Focus();
                        break;
                    case ReplyPageViewModelBase.SubmitResultType.KnownError:
                    case ReplyPageViewModelBase.SubmitResultType.NoImageError:
                    case ReplyPageViewModelBase.SubmitResultType.UnknownError:
                        break;
                }
            };

            _edit = new ApplicationBarIconButton(new Uri("Assets/Appbar/appbar.edit.png", UriKind.Relative)) { Text = AppResources.ApplicationBar_Edit };
            _edit.Click += (sender, e) => _viewModel.EditReply();

            ApplicationBarMenuItem bottom = new ApplicationBarMenuItem(AppResources.ApplicationBar_ScrollToBottom);
            bottom.Click += (sender, e) => ScrollToBottom();

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

        private void PostsPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            UpdateSplitHeight(e.Orientation);
        }

        private void UpdateSplitHeight(PageOrientation orientation)
        {
            switch (orientation)
            {
                case PageOrientation.Portrait:
                case PageOrientation.PortraitDown:
                case PageOrientation.PortraitUp:
                case PageOrientation.None:
                    SplittingPane.SplitRatio = 0.5;
                    break;
                case PageOrientation.Landscape:
                case PageOrientation.LandscapeLeft:
                case PageOrientation.LandscapeRight:
                    SplittingPane.SplitRatio = 0.75;
                    break;
            }
        }

        private void UpdateWatchButton()
        {
            if (_viewModel.IsWatchlisted)
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

        private void ScrollTargetLoaded(object sender, PostViewModel target)
        {
            _viewModel.AllPosts.Flush();

            MainPivot.SelectedIndex = 0;
            TextLLS.UpdateLayout();
            TextLLS.ScrollTo(target);
        }

        private void CaptchaFocused(object sender, EventArgs e)
        {
            Focus();
            // We have to dispatch to fix a SIP scrollviewer glitch
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                CaptchaTextBox.Focus();
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            UpdateWatchButton();
            UpdateSplitHeight(Orientation);

            if (e.NavigationMode == NavigationMode.Back && PostsPageViewModel.ForceReload)
            {
                ScrollToBottom();
                PostsPageViewModel.ForceReload = false;
            }
        }

        private void ScrollToBottom()
        {
            _viewModel.InitialUpdateTask.ContinueWith(task =>
            {
                if (MainPivot.SelectedIndex == 0)
                {
                    _viewModel.AllPosts.Flush();

                    if (_viewModel.AllPosts.Count > 0)
                    {
                        TextLLS.UpdateLayout();
                        TextLLS.ScrollTo(_viewModel.AllPosts.Last());
                    }
                }
                else
                {
                    _viewModel.ImagePosts.Flush();
                    if (_viewModel.ImagePosts.Count > 0)
                    {
                        ImageLLS.UpdateLayout();
                        ImageLLS.ScrollTo(_viewModel.ImagePosts.Last());
                    }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void ViewStateChanged(object sender, EventArgs e)
        {
            ApplicationBar.Buttons.Clear();
            switch (_viewModel.ViewState)
            {
                case PostsPageViewModel.PostsPageViewState.None:
                case PostsPageViewModel.PostsPageViewState.Quotes:
                    if (CriticalSettingsManager.Current.EnableManualRefresh)
                    {
                        ApplicationBar.Buttons.Add(_refresh);
                    }
                    ApplicationBar.Buttons.Add(_reply);
                    ApplicationBar.Buttons.Add(_watch);
                    break;
                case PostsPageViewModel.PostsPageViewState.Reply:
                    if (CriticalSettingsManager.Current.EnableManualRefresh)
                    {
                        ApplicationBar.Buttons.Add(_refresh);
                    }
                    ApplicationBar.Buttons.Add(_send);
                    ApplicationBar.Buttons.Add(_edit);
                    break;
            }
        }

        private void CaptchaTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                CommentBox.Focus();
            }
        }

        /// <summary>
        /// This function ensures the captcha box is correctly scrolled when it gets focus,
        /// since sometimes it can be off the screen.
        /// </summary>
        private void CaptchaTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (_isFixingCaptchaFocus)
            {
                return;
            }

            Focus();
            _isFixingCaptchaFocus = true;
            
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                CaptchaTextBox.Focus();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    _isFixingCaptchaFocus = false;
                });
            });
        }

        private void ContextMenuOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var owner = (FrameworkElement)menu.Owner;
            if (owner.DataContext != menu.DataContext) menu.DataContext = owner.DataContext;
        }
    }
}