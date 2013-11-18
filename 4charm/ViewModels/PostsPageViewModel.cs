using _4charm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class PostsPageViewModel : ViewModelBase
    {
        public string PivotTitle
        {
            get { return GetProperty<string>(); }
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

        public ReplyViewModel ReplyViewModel
        {
            get { return GetProperty<ReplyViewModel>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<PostViewModel> AllPosts
        {
            get { return GetProperty<ObservableCollection<PostViewModel>>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<PostViewModel> ImagePosts
        {
            get { return GetProperty<ObservableCollection<PostViewModel>>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<PostViewModel> SelectedPosts
        {
            get { return GetProperty<ObservableCollection<PostViewModel>>(); }
            set { SetProperty(value); }
        }

        public bool ForceFastLoading { get; set; }

        private Thread _thread;
        private HashSet<ulong> _seenPosts;
        private Task _initialLoadTask = null;
        private Action _filtered;

        public Task OnNavigatedTo(string boardName, ulong threadID, bool doScroll, Action after, Action filtered)
        {
            _thread = ThreadCache.Current.EnforceBoard(boardName).EnforceThread(threadID);
            _seenPosts = new HashSet<ulong>();
            _filtered = filtered;

            AllPosts = new ObservableCollection<PostViewModel>();
            ImagePosts = new ObservableCollection<PostViewModel>();
            SelectedPosts = new ObservableCollection<PostViewModel>();
            PivotTitle = "/" + boardName + "/ - " + (string.IsNullOrEmpty(_thread.Subject) ? _thread.Number + "" : _thread.Subject);
            ReplyViewModel = new ReplyViewModel(_thread);

            _initialLoadTask = InsertPosts(_thread.Posts.Values.ToList(), doScroll);
            return Update(doScroll).ContinueWith(t => after(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Refresh || e.NavigationMode == NavigationMode.Reset)
            {
                foreach (PostViewModel pvm in AllPosts) pvm.UnloadImage();
                foreach (PostViewModel pvm in ImagePosts) pvm.UnloadImage();
                foreach (PostViewModel pvm in SelectedPosts) pvm.UnloadImage();
            }
        }

        public async Task Update(bool bulkInsert = false)
        {
            IsLoading = true;

            List<Post> posts;
            try
            {
                posts = await _thread.GetPostsAsync();
            }
            catch
            {
                IsLoading = false;
                IsError = AllPosts.Count == 0;
                return;
            }

            IsLoading = false;
            IsError = false;

            await _initialLoadTask;
            await InsertPosts(posts, bulkInsert);
        }

        private async void Filter(ulong post)
        {
            IEnumerable<PostViewModel> posts = AllPosts.Where(x => x.Number == post || x.QuotesPost(post)).Select(x => new PostViewModel(x._post, Filter)).ToList();
            
            SelectedPosts = new ObservableCollection<PostViewModel>();
            _filtered();

            int j = 0;
            foreach(PostViewModel pvm in posts)
            {
                SelectedPosts.Add(pvm);
                if (j < 15) await Task.Delay(100);
                else if (j % 10 == 0) await Task.Delay(30);
                j++;
            }
        }

        private async Task InsertPosts(List<Post> posts, bool bulkInsert)
        {
            int i = 0;
            foreach(Post post in posts)
            {
                if (!_seenPosts.Contains(post.Number))
                {
                    _seenPosts.Add(post.Number);

                    PostViewModel pvm = new PostViewModel(post, Filter);
                    AllPosts.Add(pvm);
                    if (pvm.HasImage) ImagePosts.Add(new PostViewModel(post, null));

                    if (!bulkInsert && !ForceFastLoading)
                    {
                        if (i < 15) await Task.Delay(100);
                        else if (i % 10 == 0) await Task.Delay(30);
                    }
                    i++;
                }
            }
        }
    }
}
