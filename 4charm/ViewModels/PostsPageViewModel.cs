using _4charm.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Linq;
using System;
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

        private Thread _thread;
        private HashSet<ulong> _seenPosts;
        private Task _initialLoadTask = null;
        private Action _filtered;

        public void OnNavigatedTo(string boardName, ulong threadID, Action after, Action filtered)
        {
            _thread = ThreadCache.Current.EnforceBoard(boardName).EnforceThread(threadID);
            _seenPosts = new HashSet<ulong>();
            _filtered = filtered;

            AllPosts = new ObservableCollection<PostViewModel>();
            ImagePosts = new ObservableCollection<PostViewModel>();
            SelectedPosts = new ObservableCollection<PostViewModel>();
            PivotTitle = "/" + boardName + "/ - " + (string.IsNullOrEmpty(_thread.Subject) ? _thread.Number + "" : _thread.Subject);

            _initialLoadTask = InsertPosts(_thread.Posts.Values.ToList());
            Update().ContinueWith(t => after(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.IsNavigationInitiator)
            {
                foreach (PostViewModel pvm in AllPosts) pvm.UnloadImage();
                foreach (PostViewModel pvm in ImagePosts) pvm.UnloadImage();
                foreach (PostViewModel pvm in SelectedPosts) pvm.UnloadImage();
            }
        }

        public async Task Update()
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
                return;
            }

            IsLoading = false;
            await _initialLoadTask.ContinueWith(t => InsertPosts(posts), TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void Filter(ulong post)
        {
            SelectedPosts = new ObservableCollection<PostViewModel>(AllPosts.Where(x => x.Number == post || x.QuotesPost(post)).Select(x => new PostViewModel(x._post, Filter)));
            _filtered();
        }

        private async Task InsertPosts(List<Post> posts)
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

                    if (i < 15) await Task.Delay(100);
                    else if (i % 10 == 0) await Task.Delay(1);
                    i++;
                }
            }
        }
    }
}
