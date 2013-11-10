using _4charm.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace _4charm.ViewModels
{
    class ImageViewerPageViewModel : ViewModelBase
    {
        public bool IsLoading
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<object> ImagePosts
        {
            get { return GetProperty<ObservableCollection<object>>(); }
            set { SetProperty(value); }
        }

        private Thread _thread;
        private HashSet<ulong> _seenPosts;
        private bool _showLoading;

        public void OnNavigatedTo(string boardName, ulong threadID, bool skipped)
        {
            _thread = ThreadCache.Current.EnforceBoard(boardName).EnforceThread(threadID);
            _showLoading = skipped && (ulong)_thread.Posts.Where(x => x.Value.RenamedFileName != 0).Count() < _thread.ImageCount + 1;
            _seenPosts = new HashSet<ulong>();

            ImagePosts = new ObservableCollection<object>();

            IEnumerable<Post> _posts = _thread.Posts.Values.Where(x => x.RenamedFileName != 0);
            foreach (Post post in _posts)
            {
                _seenPosts.Add(post.Number);
            }
            ImagePosts = new ObservableCollection<object>(_posts.Select(x => new PostViewModel(x, null)));

            Task t = Update();
        }

        public async Task Update()
        {
            if(_showLoading) IsLoading = true;

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
            foreach (Post post in posts)
            {
                if (!_seenPosts.Contains(post.Number) && post.RenamedFileName != 0)
                {
                    _seenPosts.Add(post.Number);
                    ImagePosts.Add(new PostViewModel(post, null));
                }
            }
        }
    }
}
