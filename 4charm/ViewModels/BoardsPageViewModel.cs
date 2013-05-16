using _4charm.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class BoardsPageViewModel : ViewModelBase
    {
        public ObservableCollection<BoardViewModel> Favorites
        {
            get { return CriticalSettingsManager.Current.Favorites; }
        }

        // This one uses a settable property since we delay-load to improve startup perf
        public ObservableCollection<ThreadViewModel> Watchlist
        {
            get { return GetProperty<ObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<ThreadViewModel> History
        {
            get { return GetProperty<ObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<BoardViewModel> All
        {
            get { return CriticalSettingsManager.Current.Boards; }
        }

        public bool HasFavorites
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool HasWatchlist
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public bool HasHistory
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public TilePickerViewModel TilePicker
        {
            get { return GetProperty<TilePickerViewModel>(); }
            set { SetProperty(value); }
        }

        public BoardsPageViewModel()
        {
            TilePicker = new TilePickerViewModel();
            Watchlist = new ObservableCollection<ThreadViewModel>();
            History = new ObservableCollection<ThreadViewModel>();
        }

        public async void OnNavigatedTo()
        {
            ViewedCollectionChanged(null, null);

            await Task.Delay(200);

            Watchlist = TransitorySettingsManager.Current.Watchlist;
            History = TransitorySettingsManager.Current.History;

            Favorites.CollectionChanged += ViewedCollectionChanged;
            Watchlist.CollectionChanged += ViewedCollectionChanged;
            History.CollectionChanged += ViewedCollectionChanged;

            ViewedCollectionChanged(null, null);
        }

        void ViewedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HasFavorites = Favorites.Count > 0;
            HasHistory = History.Count > 0;
            HasWatchlist = Watchlist.Count > 0;
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            Favorites.CollectionChanged -= ViewedCollectionChanged;
            Watchlist.CollectionChanged -= ViewedCollectionChanged;
            History.CollectionChanged -= ViewedCollectionChanged;

            if (e.IsNavigationInitiator)
            {
                foreach (BoardViewModel bvm in Favorites) bvm.UnloadImage();
                foreach (BoardViewModel bvm in All) bvm.UnloadImage();
                foreach (ThreadViewModel tvm in Watchlist) tvm.UnloadImage();
                foreach (ThreadViewModel tvm in History) tvm.UnloadImage();
            }
        }
    }
}
