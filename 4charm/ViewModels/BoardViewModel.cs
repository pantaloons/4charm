using _4charm.Controls;
using _4charm.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace _4charm.ViewModels
{
    class BoardViewModel : ViewModelBase, IComparable<BoardViewModel>
    {
        public static event EventHandler<Board> NewBoardAdded = delegate { };

        public Uri WideURI
        {
            get { return GetProperty<Uri>(); }
            set { SetProperty(value); }
        }

        public Uri IconURI
        {
            get { return GetProperty<Uri>(); }
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

        private BoardViewModel()
        {
        }

        public BoardViewModel(Board b)
        {
            _board = b;
            Name = b.Name;
            DisplayName = "/" + b.Name + "/";
            Description = "/" + b.Name + "/ - " + b.Description;
            WideURI = b.WideURI;
            IconURI = b.IconURI;
            IsNSFW = b.IsNSFW;

            AddToFavorites = new ModelCommand(DoAddToFavorites);
            RemoveFromFavorites = new ModelCommand(DoRemoveFromFavorites);
            PinToStart = new ModelCommand(async () => await DoPinToStart());
            Navigated = new ModelCommand(DoNavigated);
        }

        private void DoAddToFavorites()
        {
            Board board = CriticalSettingsManager.Current.Favorites.FirstOrDefault(x => x.Name == Name);
            if (board == null)
            {
                CriticalSettingsManager.Current.Favorites.Add(_board);
                NotifyPropertyChanged("IsFavorite");
                NewBoardAdded(this, _board);
            }
        }

        private void DoRemoveFromFavorites()
        {
            Board board = CriticalSettingsManager.Current.Favorites.FirstOrDefault(x => x.Name == Name);
            if (board != null)
            {
                CriticalSettingsManager.Current.Favorites.Remove(board);
                NotifyPropertyChanged("IsFavorite");
            }
        }

        private async Task DoPinToStart()
        {
            TilePicker.TileResult result = await new TilePicker().ShowAsync(Name);
            switch (result)
            {
                case TilePicker.TileResult.ImageOption:
                     _board.PinToStart(Board.TileType.Image);
                    break;
                case TilePicker.TileResult.TextOption:
                    _board.PinToStart(Board.TileType.Text);
                    break;
                default:
                    break;
            }
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
