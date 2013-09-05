using _4charm.Models;
using System.Linq;
using System.Windows.Media;

namespace _4charm.ViewModels
{
    class ThreadViewModel : ImageViewModelBase
    {
        public string BoardName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public ulong Number
        {
            get { return GetProperty<ulong>(); }
            set { SetProperty(value); }
        }

        public Brush Background
        {
            get { return GetProperty<Brush>(); }
            set { SetProperty(value); }
        }

        public PostViewModel InitialPost
        {
            get { return GetProperty<PostViewModel>(); }
            set { SetProperty(value); }
        }

        public bool IsWatchlisted
        {
            get { return TransitorySettingsManager.Current.Watchlist.Count(x => x.BoardName == BoardName && x.Number == Number) > 0; }
        }

        public ThreadViewModel(Thread t)
        {
            BoardName = t.Board.Name;
            Number = t.Number;
            Background = t.Board.Brush;

            Post initial = t.Posts.FirstOrDefault().Value;
            if (initial != null) InitialPost = new PostViewModel(initial, null);
        }

        public override void LoadImage(int displayWidth = 100)
        {
            if (InitialPost != null) InitialPost.LoadImage(displayWidth);
        }

        public override void UnloadImage()
        {
            if (InitialPost != null) InitialPost.UnloadImage();
        }
    }
}
