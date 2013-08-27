using _4charm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
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

        public bool IsError
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<ThreadViewModel> Threads
        {
            get { return GetProperty<ObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public ObservableCollection<ThreadViewModel> ImageThreads
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

        private Task<Task> _initialPostsTask;
        private Task _extraPostsTask;
        private bool _isExtraQueued;
        private CancellationTokenSource _extraPostsCanceller;

        private bool _initialized;

        public void OnNavigatedTo(string boardName)
        {
            if (!_initialized)
            {
                _board = ThreadCache.Current.EnforceBoard(boardName);

                PivotTitle = _board.DisplayName;
                Name = _board.Name;
                IsLoading = false;
                Watchlist = new ObservableCollection<ThreadViewModel>();
                Threads = new ObservableCollection<ThreadViewModel>();
                ImageThreads = new ObservableCollection<ThreadViewModel>();

                Reload();

                _initialized = true;
            }
        }

        public void OnWatchlistNavigated()
        {
            Watchlist = new ObservableCollection<ThreadViewModel>(TransitorySettingsManager.Current.Watchlist.Where(x => x.BoardName == _board.Name));
            TransitorySettingsManager.Current.Watchlist.CollectionChanged += Watchlist_CollectionChanged;
        }

        public void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back || e.NavigationMode == NavigationMode.Refresh || e.NavigationMode == NavigationMode.Reset)
            {
                foreach (ThreadViewModel tvm in Threads) tvm.UnloadImage();
                foreach (ThreadViewModel tvm in ImageThreads) tvm.UnloadImage();
                foreach (ThreadViewModel tvm in Watchlist) tvm.UnloadImage();
            }
        }

        public void OnRemovedFromJournal()
        {
            TransitorySettingsManager.Current.Watchlist.CollectionChanged -= Watchlist_CollectionChanged;
        }

        public Task Reload()
        {
            if (_reloadTask == null || _reloadTask.IsCompleted) _reloadTask = ReloadAsync();
            return _reloadTask;
        }

        private async Task ReloadAsync()
        {
            if(_extraPostsCanceller != null) _extraPostsCanceller.Cancel();

            Threads.Clear();
            ImageThreads.Clear();
            IsLoading = true;

            List<_4charm.Models.Thread> threads;
            try
            {
                threads = await _board.GetThreadsAsync();
            }
            catch
            {
                IsLoading = false;
                IsError = true;
                return;
            }

            IsLoading = false;
            IsError = false;

            _initialPostsTask = new Task<Task>(async () =>
            {
                for (int j = 0; j < Math.Min(15, threads.Count); j++)
                {
                    _4charm.Models.Thread thread = threads[j];
                    if (!thread.IsSticky || CriticalSettingsManager.Current.ShowStickies)
                    {
                        Threads.Add(new ThreadViewModel(thread));
                        ImageThreads.Add(new ThreadViewModel(thread));
                        await Task.Delay(100);
                    }
                }
            });

            CancellationTokenSource local = new CancellationTokenSource();
            _extraPostsCanceller = local;
            _isExtraQueued = false;
            _extraPostsTask = new Task(async () =>
            {
                for(int j = 15; j < threads.Count; j++)
                {
                    _4charm.Models.Thread thread = threads[j];
                    if (!thread.IsSticky || CriticalSettingsManager.Current.ShowStickies)
                    {
                        if (local.Token.IsCancellationRequested)
                        {
                            return;
                        }
                        Threads.Add(new ThreadViewModel(thread));
                        ImageThreads.Add(new ThreadViewModel(thread));
                        if (j % 10 == 0) await Task.Delay(100);
                    }
                }
            }, local.Token);

            _initialPostsTask.Start(TaskScheduler.FromCurrentSynchronizationContext());
            await _initialPostsTask.Unwrap();
        }

        public void FinishInsertingPosts()
        {
            if (_isExtraQueued) return;
            _isExtraQueued = true;

            TaskScheduler sched = TaskScheduler.FromCurrentSynchronizationContext();
            _initialPostsTask.Unwrap().ContinueWith(t =>
            {
                _extraPostsTask.Start(sched);
            });
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
                    throw new NotImplementedException();
            }
        }
    }
}
