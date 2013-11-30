using _4charm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace _4charm.ViewModels
{
    class ThreadsPageViewModel : PageViewModelBase
    {
        // We use this hack to notify the threads page to reload
        // after creating a new thread. The thread creation page
        // calls GoBack() and then navigated to the new post page,
        // but if the user hits back we want to show their thread
        // at the top of the list.
        internal static bool ForceReload = false;

        public string PivotTitle
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public int SelectedIndex
        {
            get { return GetProperty<int>(); }
            set
            {
                SetProperty(value);
                SelectedIndexChanged();
            }
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

        public DelayLoadingObservableCollection<ThreadViewModel> Threads
        {
            get { return GetProperty<DelayLoadingObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingObservableCollection<ThreadViewModel> ImageThreads
        {
            get { return GetProperty<DelayLoadingObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingObservableCollection<ThreadViewModel> Watchlist
        {
            get { return GetProperty<DelayLoadingObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        private bool _removedFromJournal;
        private Board _board;
        private Task _downloadTask;

        public override void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
            _board = ThreadCache.Current.EnforceBoard(arguments["board"]);

            PivotTitle = _board.DisplayName;
            Name = _board.Name;
            IsLoading = false;
            Threads = new DelayLoadingObservableCollection<ThreadViewModel>(100, false);
            Watchlist = new DelayLoadingObservableCollection<ThreadViewModel>(100, true);
            ImageThreads = new DelayLoadingObservableCollection<ThreadViewModel>(40, true);

            ReloadThreads();

            App.InitialFrameRenderedTask.ContinueWith(task =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (_removedFromJournal)
                    {
                        return;
                    }

                    Watchlist.AddRange(TransitorySettingsManager.Current.Watchlist.Where(x => x.Board.Name == _board.Name).Select(x => new ThreadViewModel(x)));
                    TransitorySettingsManager.Current.Watchlist.CollectionChanged += Watchlist_CollectionChanged;
                });
            }, TaskScheduler.Current);
        }

        public override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (ForceReload)
            {
                ForceReload = false;
                ReloadThreads();
            }

            SelectedIndexChanged();
        }

        public override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            Threads.IsPaused = true;
            ImageThreads.IsPaused = true;
            Watchlist.IsPaused = true;
        }

        public override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            _removedFromJournal = true;

            TransitorySettingsManager.Current.Watchlist.CollectionChanged -= Watchlist_CollectionChanged;
        }

        public void ReloadThreads()
        {
            if (_downloadTask != null && !_downloadTask.IsCompleted)
            {
                return;
            }

            Threads.Clear();
            ImageThreads.Clear();
            IsLoading = true;

            Task<List<Models.Thread>> download = _board.GetThreadsAsync();
            _downloadTask = download.ContinueWith(task =>
            {
                IsLoading = false;
                if (task.IsFaulted)
                {               
                    IsError = true;
                    return;
                }

                IEnumerable<ThreadViewModel> threads = task.Result
                    .Where(x => !x.IsSticky || CriticalSettingsManager.Current.ShowStickies)
                    .Select(x => new ThreadViewModel(x));
                Threads.AddRange(threads);
                ImageThreads.AddRange(threads);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void ClearWatchlist()
        {
            for (int i = 0; i < TransitorySettingsManager.Current.Watchlist.Count; i++)
            {
                if (TransitorySettingsManager.Current.Watchlist[i].Board.Name == Name)
                {
                    TransitorySettingsManager.Current.Watchlist.RemoveAt(i);
                    i--;
                }
            }
        }

        private void SelectedIndexChanged()
        {
            Threads.IsPaused = true;
            ImageThreads.IsPaused = true;
            Watchlist.IsPaused = true;

            if (SelectedIndex == 0) Threads.IsPaused = false;
            else if (SelectedIndex == 1) Watchlist.IsPaused = false;
            else if (SelectedIndex == 2) ImageThreads.IsPaused = false;
        }

        private void Watchlist_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (((Models.Thread)e.NewItems[0]).Board.Name == _board.Name)
                    {
                        Watchlist.Add(new ThreadViewModel((Models.Thread)e.NewItems[0]));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (((Models.Thread)e.OldItems[0]).Board.Name == _board.Name)
                    {
                        ThreadViewModel tvm = Watchlist.FirstOrDefault(x => x.Number == ((Models.Thread)e.OldItems[0]).Number);
                        if (tvm != null)
                        {
                            Watchlist.Remove(tvm);
                        }
                    }
                    
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }
    }
}
