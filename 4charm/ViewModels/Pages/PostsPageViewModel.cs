using _4charm.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace _4charm.ViewModels
{
    class PostsPageViewModel : ReplyPageViewModelBase
    {
        // We use this hack to notify the posts page to reload
        // after creating a new post. The post creation page
        // calls GoBack() and we want to show the post immediately.
        internal static bool ForceReload = false;

        public enum PostsPageViewState
        {
            None,
            Quotes,
            Reply
        };

        public PostsPageViewState ViewState
        {
            get { return GetProperty<PostsPageViewState>(); }
            set { SetProperty(value); }
        }

        public string PivotTitle
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public int SelectedIndex
        {
            get { return GetProperty<int>(); }
            set
            {
                SetProperty(value);
                SelectedIndexChanged();
            }
        }

        public bool IsSpecialRegionExpanded
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool IsLoading
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool IsError
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool IsWatchlisted
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingObservableCollection<PostViewModel> AllPosts
        {
            get { return GetProperty<DelayLoadingObservableCollection<PostViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingObservableCollection<PostViewModel> ImagePosts
        {
            get { return GetProperty<DelayLoadingObservableCollection<PostViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingObservableCollection<PostViewModel> SelectedPosts
        {
            get { return GetProperty<DelayLoadingObservableCollection<PostViewModel>>(); }
            set { SetProperty(value); }
        }

        public Brush Background
        {
            get { return GetProperty<Brush>(); }
            set { SetProperty(value); }
        }

        public int CommentSelectionStart
        {
            get { return GetProperty<int>(); }
            set { SetProperty(value); }
        }

        public string QuotedTitle
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public Visibility ReplyAreaVisibility
        {
            get { return GetProperty<Visibility>(); }
            set { SetProperty(value); }
        }

        public Visibility QuoteAreaVisibility
        {
            get { return GetProperty<Visibility>(); }
            set { SetProperty(value); }
        }

        public Task InitialUpdateTask
        {
            get { return GetProperty<Task>(); }
            set { SetProperty(value); }
        }

        public event EventHandler ViewStateChanged;
        public event EventHandler<PostViewModel> ScrollTargetLoaded;
        public event EventHandler CaptchaFocused;

        private Thread _thread;
        private ulong _scrollTarget;

        private HashSet<ulong> _seenPosts;
        private Task _updateTask;
        private ulong _quotedPost;

        private System.Threading.Timer _refreshTimer;

        public PostsPageViewModel()
        {
            ReloadCaptcha = new ModelCommand(async () =>
            {
                CaptchaText = "";
                CaptchaFocused(this, null);
                await DoLoadCaptcha();
            });
        }

        public override void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
            base.Initialize(arguments, e);

            _thread = ThreadCache.Current.EnforceBoard(arguments["board"]).EnforceThread(ulong.Parse(arguments["thread"]));
            _seenPosts = new HashSet<ulong>();

            if (arguments.ContainsKey("post"))
            {
                _scrollTarget = ulong.Parse(arguments["post"]);
            }

            ViewState = PostsPageViewState.None;
            PivotTitle = _thread.Board.DisplayName + " - " + (string.IsNullOrEmpty(_thread.Subject) ? _thread.Number + "" : _thread.Subject);
            IsWatchlisted = TransitorySettingsManager.Current.Watchlist.Count(x => x.Board.Name == _thread.Board.Name && x.Number == _thread.Number) > 0;

            AllPosts = new DelayLoadingObservableCollection<PostViewModel>(100, false, 15, 100, 10);
            ImagePosts = new DelayLoadingObservableCollection<PostViewModel>(25, true, 35, 100, 10);
            SelectedPosts = new DelayLoadingObservableCollection<PostViewModel>(50, true, 3, 100, 10);

            QuotedTitle = "";
            Background = _thread.Board.Brush;

            QuoteAreaVisibility = Visibility.Collapsed;
            ReplyAreaVisibility = Visibility.Collapsed;

            Thread thread = TransitorySettingsManager.Current.History.FirstOrDefault(x => x.Board.Name == _thread.Board.Name && x.Number == _thread.Number);
            if (thread != null)
            {
                TransitorySettingsManager.Current.History.Remove(thread);
            }
            TransitorySettingsManager.Current.History.Insert(0, _thread);

            InsertPostList(_thread.Posts.Values);
            InitialUpdateTask = Update();
            System.Diagnostics.Debug.WriteLine("iupdate");

            if (!CriticalSettingsManager.Current.EnableManualRefresh)
            {
                _refreshTimer = new System.Threading.Timer(state =>
                {
                    Deployment.Current.Dispatcher.BeginInvoke(async () => await Update());
                }, null, 30 * 1000, 30 * 1000);
            }

            DoLoadCaptcha().ContinueWith(result =>
            {
                throw result.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
        }

        public override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SelectedIndexChanged();
            
            if (e.NavigationMode == NavigationMode.Back && ForceReload)
            {
                OnSubmitSuccess("");
                System.Diagnostics.Debug.WriteLine("vm nav too");
            }
        }

        public override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            AllPosts.IsPaused = true;
            ImagePosts.IsPaused = true;
            SelectedPosts.IsPaused = true;
        }

        public override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            if (_refreshTimer != null) _refreshTimer.Dispose();
            _refreshTimer = null;
        }

        public override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (ViewState != PostsPageViewState.None && !IsPosting)
            {
                GoToState(PostsPageViewState.None);
                e.Cancel = true;
            }
        }

        public void ToggleWatchlisted()
        {
            Thread thread = TransitorySettingsManager.Current.Watchlist.FirstOrDefault(x => x.Board.Name == _thread.Board.Name && x.Number == _thread.Number);
            if (thread != null)
            {
                TransitorySettingsManager.Current.Watchlist.Remove(thread);
                IsWatchlisted = false;
            }
            else
            {
                TransitorySettingsManager.Current.Watchlist.Add(_thread);
                IsWatchlisted = true;
            }
        }

        public void EditReply()
        {
            Navigate(new Uri(String.Format("/Views/NewThreadPage.xaml?board={0}&thread={1}&token={2}&captcha={3}&comment={4}",
                Uri.EscapeUriString(_thread.Board.Name),
                Uri.EscapeUriString(_thread.Number + ""),
                Uri.EscapeUriString(_token),
                Uri.EscapeUriString(CaptchaText),
                Uri.EscapeUriString(Comment)),
                UriKind.Relative));
        }

        public Task ForceUpdate()
        {
            if (_updateTask == null || _updateTask.IsCompleted)
            {
                return Update();
            }
            else
            {
                // We queue another update after the existing one, since the
                // existing one might not pick up the new post.
                _updateTask = _updateTask.ContinueWith(async task =>
                {
                    await Update();
                }, TaskContinuationOptions.ExecuteSynchronously).Unwrap();

                return _updateTask;
            }
        }

        public Task Update()
        {
            if (_updateTask != null && !_updateTask.IsCompleted)
            {
                return _updateTask;
            }

            IsLoading = true;

            Task<List<Post>> download = _thread.GetPostsAsync();
            _updateTask = download.ContinueWith(task =>
            {
                System.Diagnostics.Debug.WriteLine("download done!");
                if (!task.IsFaulted)
                {
                    IsError = false;
                }
                else
                {
                    IsLoading = false;
                    IsError = AllPosts.Count == 0;
                    return;
                }

                InsertPostList(task.Result);
                IsLoading = false;
            }, TaskContinuationOptions.ExecuteSynchronously);

            return _updateTask;
        }

        private void InsertPostList(IEnumerable<Post> posts)
        {
            IEnumerable<PostViewModel> newPosts = posts
                .Where(x => !_seenPosts.Contains(x.Number))
                .Select(x => new PostViewModel(x)
                {
                    Tapped = new ModelCommand(() => PostTapped(x.Number)),
                    NumberTapped = new ModelCommand(() => OpenQuoteRegion(x.Number)),
                    ViewQuotes = new ModelCommand(() => OpenQuoteRegion(x.Number)),
                    QuoteTapped = new ModelCommand<ulong>(postID => OpenQuoteRegion(postID))
                }).ToList();

            bool hasScrollTarget = false;
            PostViewModel scrollTarget = null;
            foreach (PostViewModel post in newPosts)
            {
                _seenPosts.Add(post.Number);
                if (post.Number == _scrollTarget)
                {
                    hasScrollTarget = true;
                    scrollTarget = post;
                }
            }

            AllPosts.AddRange(newPosts);
            ImagePosts.AddRange(newPosts.Where(x => x.HasImage));
            SelectedPosts.AddRange(newPosts.Where(x => x.Number == _quotedPost || x.QuotesPost(_quotedPost)));
            
            if (hasScrollTarget)
            {
                AllPosts.Flush();
                ScrollTargetLoaded(this, scrollTarget);
            }
        }

        private void SelectedIndexChanged()
        {
            AllPosts.IsPaused = true;
            ImagePosts.IsPaused = true;
            SelectedPosts.IsPaused = true;

            if (SelectedIndex == 0) AllPosts.IsPaused = false;
            else if (SelectedIndex == 1) ImagePosts.IsPaused = false;

            if (IsSpecialRegionExpanded && ViewState == PostsPageViewState.Quotes) SelectedPosts.IsPaused = false;
        }

        private void PostTapped(ulong postID)
        {
            if (ViewState != PostsPageViewState.Reply || IsPosting)
            {
                return;
            }

            Comment += ">>" + postID + "\n";
            CommentSelectionStart = Comment.Length;
        }

        public void OpenReplyRegion()
        {
            GoToState(PostsPageViewState.Reply);
        }

        private void OpenQuoteRegion(ulong postID)
        {
            if (IsPosting)
            {
                return;
            }

            // We have to use .All() to get posts that are queued, but not yet inserted too.
            IEnumerable<PostViewModel> posts = AllPosts.All().Where(x => x.Number == postID || x.QuotesPost(postID));

            SelectedPosts.Clear();
            SelectedPosts.IsPaused = false;
            SelectedPosts.AddRange(posts);

            QuotedTitle = ">>" + postID;

            _quotedPost = postID;

            GoToState(PostsPageViewState.Quotes);
        }

        private void GoToState(PostsPageViewState target)
        {
            if (target == ViewState)
            {
                return;
            }

            IsSpecialRegionExpanded = target == PostsPageViewState.Quotes || target == PostsPageViewState.Reply;
            SelectedPosts.IsPaused = target != PostsPageViewState.Quotes;

            if (target != PostsPageViewState.Quotes)
            {
                _quotedPost = 0;
            }

            ViewState = target;

            ReplyAreaVisibility = ViewState == PostsPageViewState.Reply ? Visibility.Visible : Visibility.Collapsed;
            QuoteAreaVisibility = ViewState == PostsPageViewState.Quotes ? Visibility.Visible : Visibility.Collapsed;

            ViewStateChanged(this, null);

            AllPosts.IsPaused = true;
            ImagePosts.IsPaused = true;

            Task.Delay(500).ContinueWith(task =>
            {
                SelectedIndexChanged();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public async Task<SubmitResultType> Submit()
        {
            return await SubmitInternal(_thread.Number);
        }

        protected override void OnSubmitSuccess(string result)
        {
            DoLoadCaptcha().ContinueWith(eresult =>
            {
                throw eresult.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);

            Comment = "";
            CaptchaText = "";

            GoToState(PostsPageViewState.None);

            InitialUpdateTask = ForceUpdate();
        }
    }
}
