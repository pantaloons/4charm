using _4charm.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class BoardsPageViewModel : PageViewModelBase
    {
        public int SelectedIndex
        {
            get { return GetProperty<int>(); }
            set
            {
                SetProperty(value);
                SelectedIndexChanged();
            }
        }

        public DelayLoadingObservableCollection<BoardViewModel> Favorites
        {
            get { return GetProperty<DelayLoadingObservableCollection<BoardViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingObservableCollection<ThreadViewModel> Watchlist
        {
            get { return GetProperty<DelayLoadingObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingObservableCollection<ThreadViewModel> History
        {
            get { return GetProperty<DelayLoadingObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingObservableCollection<BoardViewModel> All
        {
            get { return GetProperty<DelayLoadingObservableCollection<BoardViewModel>>(); }
            set { SetProperty(value); }
        }

        private bool _removedFromJournal;

        public BoardsPageViewModel()
        {
            Favorites = new DelayLoadingObservableCollection<BoardViewModel>(100, true, 15, 100, 10);
            All = new DelayLoadingObservableCollection<BoardViewModel>(50, true, 15, 100, 10);
            Watchlist = new DelayLoadingObservableCollection<ThreadViewModel>(100, true, 15, 100, 10);
            History = new DelayLoadingObservableCollection<ThreadViewModel>(100, true, 15, 100, 10);

            Favorites.AddRange(CriticalSettingsManager.Current.Favorites.Select(x => new BoardViewModel(x)));
            Favorites.Flush(2);
            All.AddRange(CriticalSettingsManager.Current.Boards.Select(x => new BoardViewModel(x)));

            CriticalSettingsManager.Current.Favorites.CollectionChanged += FavoritesChanged;
            CriticalSettingsManager.Current.Boards.CollectionChanged += AllChanged;

            App.InitialFrameRenderedTask.ContinueWith(task =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (_removedFromJournal)
                    {
                        return;
                    }

                    Watchlist.AddRange(TransitorySettingsManager.Current.Watchlist.Select(x => new ThreadViewModel(x)));
                    History.AddRange(TransitorySettingsManager.Current.History.Select(x => new ThreadViewModel(x)));

                    TransitorySettingsManager.Current.Watchlist.CollectionChanged += WatchlistChanged;
                    TransitorySettingsManager.Current.History.CollectionChanged += HistoryChanged;
                });
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            SelectedIndexChanged();
        }

        public override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Favorites.IsPaused = true;
            All.IsPaused = true;
            Watchlist.IsPaused = true;
            History.IsPaused = true;
        }

        public override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            _removedFromJournal = true;

            CriticalSettingsManager.Current.Favorites.CollectionChanged -= FavoritesChanged;
            CriticalSettingsManager.Current.Boards.CollectionChanged -= AllChanged;
            TransitorySettingsManager.Current.Watchlist.CollectionChanged -= WatchlistChanged;
            TransitorySettingsManager.Current.History.CollectionChanged -= HistoryChanged;
        }

        private void FavoritesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new BoardViewModel((Board)e.NewItems[0]));
            }

            ListCollectionChanged<BoardViewModel>(Favorites, e);
        }

        private void AllChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new BoardViewModel((Board)e.NewItems[0]), e.NewStartingIndex);
            }
            
            ListCollectionChanged<BoardViewModel>(All, e);
        }

        private void HistoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new ThreadViewModel((Thread)e.NewItems[0]), e.NewStartingIndex);
            }

            ListCollectionChanged<ThreadViewModel>(History, e);
        }

        private void WatchlistChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new ThreadViewModel((Thread)e.NewItems[0]));
            }

            ListCollectionChanged<ThreadViewModel>(Watchlist, e);
        }

        private void ListCollectionChanged<T>(DelayLoadingObservableCollection<T> target, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    target.Insert(e.NewStartingIndex, (T)e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Move:
                    target.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    target.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    target.Clear();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Debug.Assert(false);
                    break;
            }
        }

        private void SelectedIndexChanged()
        {
            Favorites.IsPaused = true;
            All.IsPaused = true;
            Watchlist.IsPaused = true;
            History.IsPaused = true;

            if (SelectedIndex == 0) Favorites.IsPaused = false;
            else if (SelectedIndex == 1) Watchlist.IsPaused = false;
            else if (SelectedIndex == 2) History.IsPaused = false;
            else if (SelectedIndex == 3) All.IsPaused = false;
        }
    }
}
