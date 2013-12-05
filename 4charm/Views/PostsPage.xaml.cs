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
        private PostsPageViewModel _viewModel;

        private ApplicationBarIconButton _refresh, _watch, _reply, _send;

        public PostsPage()
        {
            InitializeComponent();
            InitializeApplicationBar();

            _viewModel = new PostsPageViewModel();
            DataContext = _viewModel;

            _viewModel.ViewStateChanged += ViewStateChanged;
            _viewModel.ScrollTargetLoaded += ScrollTargetLoaded;
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
                //BeginPostingStoryboard.Begin();
                ReplyPageViewModelBase.SubmitResultType result = await _viewModel.Submit();
                _send.IsEnabled = true;
                //BeginPostingStoryboard.Stop();

                switch(result)
                {
                    case ReplyPageViewModelBase.SubmitResultType.Success:
                        //ReplyScroller.ScrollToVerticalOffset(0);
                        await _viewModel.Update();
                        //TextLLS.ScrollTo(_viewModel.AllPosts.Last());
                        break;
                    case ReplyPageViewModelBase.SubmitResultType.EmptyCaptchaError:
                    case ReplyPageViewModelBase.SubmitResultType.WrongCatpchaError:
                        //CaptchaTextBox.Focus();
                        //ReplyScroller.ScrollToVerticalOffset(InnerReplyGrid.RowDefinitions[0].ActualHeight - 10);
                        break;
                    case ReplyPageViewModelBase.SubmitResultType.EmptyCommentError:
                        //CommentTextBox.Focus();
                        //ReplyScroller.ScrollToVerticalOffset(0);
                        break;
                    case ReplyPageViewModelBase.SubmitResultType.KnownError:
                    case ReplyPageViewModelBase.SubmitResultType.NoImageError:
                    case ReplyPageViewModelBase.SubmitResultType.UnknownError:
                        break;
                }
            };

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
            //TextLLS.UpdateLayout();
            //TextLLS.ScrollTo(target);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            UpdateWatchButton();
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
                        //TextLLS.ScrollTo(_viewModel.AllPosts.Last());
                    }
                }
                else
                {
                    _viewModel.ImagePosts.Flush();
                    if (_viewModel.ImagePosts.Count > 0)
                    {
                        //ImageLLS.ScrollTo(_viewModel.ImagePosts.Last());
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
                    break;
            }
        }

        private void ContextMenuOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            var menu = (ContextMenu)sender;
            var owner = (FrameworkElement)menu.Owner;
            if (owner.DataContext != menu.DataContext) menu.DataContext = owner.DataContext;
        }
    }
}