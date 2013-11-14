using _4charm.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Windows.Navigation;
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

            // TODO: Initialize these as empty by default, and then load afterwards.
            Watchlist = new ObservableCollection<ThreadViewModel>();
            History = new ObservableCollection<ThreadViewModel>();
        }
    }
}
