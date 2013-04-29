using _4charm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class BoardsPageViewModel : ViewModelBase
    {
        public ObservableCollection<BoardViewModel> Favorites
        {
            get { return SettingsManager.Current.Favorites; }
        }

        public ObservableCollection<ThreadViewModel> Watchlist
        {
            get { return SettingsManager.Current.Watchlist; }
        }

        public ObservableCollection<ThreadViewModel> History
        {
            get { return SettingsManager.Current.History; }
        }

        public ObservableCollection<BoardViewModel> All
        {
            get { return SettingsManager.Current.Boards; }
        }

        public BoardViewModel SelectedBoard
        {
            get { return GetProperty<BoardViewModel>(); }
            set { SetProperty(value); }
        }

        public TilePickerViewModel TilePicker
        {
            get { return GetProperty<TilePickerViewModel>(); }
            set { SetProperty(value); }
        }

        public BoardsPageViewModel()
        {
            SelectedBoard = null;
            TilePicker = new TilePickerViewModel();
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
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
