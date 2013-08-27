using _4charm.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace _4charm.ViewModels
{
    class BoardViewModel : ViewModelBase, IComparable<BoardViewModel>
    {
        public static event EventHandler<Board> NewBoardAdded = delegate { };
        public static event EventHandler<Board> BoardPinned = delegate { };

        public BitmapImage Image
        {
            get { return GetProperty<BitmapImage>(); }
            set { SetProperty(value); }
        }

        public BitmapImage Icon
        {
            get { return GetProperty<BitmapImage>(); }
            set { SetProperty(value); }
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string DisplayName
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Description
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public bool IsNSFW
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool IsFavorite
        {
            get { return _board.IsFavorite; }
        }

        public ICommand AddToFavorites
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand RemoveFromFavorites
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand PinToStart
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        public ICommand Navigated
        {
            get { return GetProperty<ICommand>(); }
            set { SetProperty(value); }
        }

        private Board _board;

        public BoardViewModel()
        {
        }

        public BoardViewModel(Board b)
        {
            _board = b;
            Name = b.Name;
            DisplayName = "/" + b.Name + "/";
            Description = "/" + b.Name + "/ - " + b.Description;
            IsNSFW = b.IsNSFW;

            AddToFavorites = new ModelCommand(DoAddToFavorites);
            RemoveFromFavorites = new ModelCommand(DoRemoveFromFavorites);
            PinToStart = new ModelCommand(DoPinToStart);
            Navigated = new ModelCommand(DoNavigated);
        }

        ~BoardViewModel()
        {
            //if (Image != null) throw new System.Exception();
            //if (Icon != null) throw new System.Exception();
        }

        private BitmapImage _loading, _loadingIcon;
        public void LoadImage()
        {
            //if (_loading != null) throw new Exception();
            _loading = new BitmapImage() { DecodePixelWidth = 440 };
            _loading.ImageOpened += ImageLoaded;
            _loading.CreateOptions = BitmapCreateOptions.BackgroundCreation;
            _loading.UriSource = _board.WideURI;
        }

        public void LoadIcon()
        {
            _loadingIcon = new BitmapImage() { DecodePixelWidth = 440 };
            _loadingIcon.ImageOpened += IconLoaded;
            _loadingIcon.CreateOptions = BitmapCreateOptions.BackgroundCreation;
            _loadingIcon.UriSource = _board.IconURI;
        }

        private void ImageLoaded(object sender, RoutedEventArgs e)
        {
            Image = _loading;
        }

        private void IconLoaded(object sender, RoutedEventArgs e)
        {
            Icon = _loadingIcon;
        }

        public void UnloadImage()
        {
            if (_loading != null)
            {
                _loading.ImageOpened -= ImageLoaded;
                _loading.UriSource = null;
                _loading = null;
            }

            if (Image != null)
            {
                Image.UriSource = null;
                Image = null;
            }
        }

        public void UnloadIcon()
        {
            if (_loadingIcon != null)
            {
                _loadingIcon.ImageOpened -= ImageLoaded;
                _loadingIcon.UriSource = null;
                _loadingIcon = null;
            }

            if (Icon != null)
            {
                Icon.UriSource = null;
                Icon = null;
            }
        }

        private void DoAddToFavorites()
        {
            BoardViewModel bvm = CriticalSettingsManager.Current.Favorites.FirstOrDefault(x => x.Name == Name);
            if (bvm == null)
            {
                BoardViewModel fav = new BoardViewModel(_board);
                CriticalSettingsManager.Current.Favorites.Add(fav);
                NotifyPropertyChanged("IsFavorite");
                NewBoardAdded(fav, _board);
            }
        }

        private void DoRemoveFromFavorites()
        {
            BoardViewModel bvm = CriticalSettingsManager.Current.Favorites.FirstOrDefault(x => x.Name == Name);
            if (bvm != null)
            {
                CriticalSettingsManager.Current.Favorites.Remove(bvm);
                NotifyPropertyChanged("IsFavorite");
            }
        }

        private void DoPinToStart()
        {
            BoardPinned(null, _board);
        }

        private void DoNavigated()
        {
            Navigate(new Uri(String.Format("/Views/ThreadsPage.xaml?board={0}", Uri.EscapeUriString(Name)), UriKind.Relative));
        }

        public int CompareTo(BoardViewModel other)
        {
            return Name.CompareTo(other.Name);
        }
    }
}
