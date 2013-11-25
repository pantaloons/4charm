using _4charm.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace _4charm.ViewModels
{
    class BoardsPageViewModel : PageViewModelBase
    {
        public ObservableCollection<BoardViewModel> Favorites
        {
            get { return GetProperty<ObservableCollection<BoardViewModel>>(); }
            set { SetProperty(value); }
        }

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
            get { return GetProperty<ObservableCollection<BoardViewModel>>(); }
            set { SetProperty(value); }
        }

        public BoardsPageViewModel()
        {
            Favorites = new ObservableCollection<BoardViewModel>(CriticalSettingsManager.Current.Favorites.Select(x => new BoardViewModel(x)));
            All = new ObservableCollection<BoardViewModel>(CriticalSettingsManager.Current.Boards.Select(x => new BoardViewModel(x)));

            CriticalSettingsManager.Current.Favorites.CollectionChanged += (sender, e) => BoardCollectionChanged(Favorites, e);
            CriticalSettingsManager.Current.Boards.CollectionChanged += (sender, e) => BoardCollectionChanged(All, e);

            // TODO: Initialize these as empty by default, and then load afterwards.
            Watchlist = new ObservableCollection<ThreadViewModel>();
            History = new ObservableCollection<ThreadViewModel>();
        }

        private void BoardCollectionChanged(ObservableCollection<BoardViewModel> target, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    target.Insert(e.NewStartingIndex, new BoardViewModel((Board)e.NewItems[0]));
                    break;
                case NotifyCollectionChangedAction.Move:
                    target.Move(e.OldStartingIndex, e.NewStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    target.RemoveAt(e.OldStartingIndex);
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Reset:
                    Debug.Assert(false);
                    break;
            }
        }
    }
}
