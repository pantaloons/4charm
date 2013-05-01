using _4charm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class ThreadsPageViewModel : ViewModelBase
    {
        public string PivotTitle
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public bool IsLoading
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<ThreadViewModel> Threads
        {
            get { return GetProperty<ObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<ThreadViewModel> Watchlist
        {
            get { return GetProperty<ObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        private Board _board;
        private Task _reloadTask;
        private bool _initialized;

        public void OnNavigatedTo(string boardName)
        {
            if (!_initialized)
            {
                _board = ThreadCache.Current.EnforceBoard(boardName);

                PivotTitle = _board.DisplayName;
                Name = _board.Name;
                IsLoading = false;
                Watchlist = new ObservableCollection<ThreadViewModel>(SettingsManager.Current.Watchlist.Where(x => x.BoardName == _board.Name));
                SettingsManager.Current.Watchlist.CollectionChanged += Watchlist_CollectionChanged;

                Threads = new ObservableCollection<ThreadViewModel>();

                Reload();

                _initialized = true;
            }
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.IsNavigationInitiator)
            {
                foreach (ThreadViewModel tvm in Threads) tvm.UnloadImage();
                foreach (ThreadViewModel tvm in Watchlist) tvm.UnloadImage();
            }
        }

        public void OnRemovedFromJournal()
        {
            SettingsManager.Current.Watchlist.CollectionChanged -= Watchlist_CollectionChanged;
        }

        public Task Reload()
        {
            if (_reloadTask == null || _reloadTask.IsCompleted) _reloadTask = ReloadAsync();
            return _reloadTask;
        }

        private async Task ReloadAsync()
        {
            Threads.Clear();
            IsLoading = true;

            List<Thread> threads;
            try
            {
                threads = await _board.GetThreadsAsync();
            }
            catch
            {
                IsLoading = false;
                return;
            }

            IsLoading = false;

            int j = 0;
            foreach (Thread thread in threads)
            {
                if (!thread.IsSticky || SettingsManager.Current.ShowStickies)
                {
                    Threads.Add(new ThreadViewModel(thread));
                    if (j < 15) await Task.Delay(100);
                    else if (j % 10 == 0) await Task.Delay(1);
                    j++;
                }
            }
        }

        private void Watchlist_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if((e.NewItems[0] as ThreadViewModel).BoardName == _board.Name) Watchlist.Add(e.NewItems[0] as ThreadViewModel);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if ((e.OldItems[0] as ThreadViewModel).BoardName == _board.Name)
                    {
                        ThreadViewModel tvm = Watchlist.FirstOrDefault(x => x.Number == (e.OldItems[0] as ThreadViewModel).Number);
                        if (tvm != null) Watchlist.Remove(tvm);
                    }
                    
                    break;
                default:
                    // TODO: Remove me?
                    throw new NotImplementedException();
            }
        }
    }
}
