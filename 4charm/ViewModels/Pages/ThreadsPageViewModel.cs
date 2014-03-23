using _4charm.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
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

        public bool IsSearching
        {
            get { return GetProperty<bool>(); }
            set { SetProperty(value); }
        }

        public string SearchText
        {
            get { return GetProperty<string>(); }
            set
            {
                SetProperty(value);
                SearchTextChanged();
            }
        }

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

        public DelayLoadingFilteredObservableCollection<ThreadViewModel> Threads
        {
            get { return GetProperty<DelayLoadingFilteredObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingFilteredObservableCollection<ThreadViewModel> ImageThreads
        {
            get { return GetProperty<DelayLoadingFilteredObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public DelayLoadingFilteredObservableCollection<ThreadViewModel> Watchlist
        {
            get { return GetProperty<DelayLoadingFilteredObservableCollection<ThreadViewModel>>(); }
            set { SetProperty(value); }
        }

        public event EventHandler SearchStateChanged;

        private bool _removedFromJournal;
        private Board _board;
        private Task _downloadTask;

        public override void Initialize(IDictionary<string, string> arguments, NavigationEventArgs e)
        {
            _board = ThreadCache.Current.EnforceBoard(arguments["board"]);

            PivotTitle = _board.DisplayName;
            Name = _board.Name;
            IsLoading = false;
            Threads = new DelayLoadingFilteredObservableCollection<ThreadViewModel>(100, false, 15, 100, 10);
            Watchlist = new DelayLoadingFilteredObservableCollection<ThreadViewModel>(100, true, 15, 100, 10);
            ImageThreads = new DelayLoadingFilteredObservableCollection<ThreadViewModel>(40, true, 15, 100, 10);

            ReloadThreads();

            App.InitialFrameRenderedTask.ContinueWith(task =>
            {
                if (_removedFromJournal)
                {
                    return;
                }

                Watchlist.AddRange(TransitorySettingsManager.Current.Watchlist.Where(x => x.Board.Name == _board.Name).Select(x => new ThreadViewModel(x)));
                TransitorySettingsManager.Current.Watchlist.CollectionChanged += Watchlist_CollectionChanged;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public override void SaveState(IDictionary<string, object> state)
        {
            state["SelectedIndex"] = SelectedIndex;
        }

        public override void RestoreState(IDictionary<string, object> state)
        {
            if (state.ContainsKey("SelectedIndex"))
            {
                SelectedIndex = (int)state["SelectedIndex"];
            }
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

        public override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (IsSearching)
            {
                IsSearching = false;
                SearchText = "";
                e.Cancel = true;
                SearchStateChanged(null, null);
            }
        }

        public override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            _removedFromJournal = true;

            TransitorySettingsManager.Current.Watchlist.CollectionChanged -= Watchlist_CollectionChanged;
        }

        public void ShowSearchBox()
        {
            SearchText = "";
            IsSearching = true;
            SearchStateChanged(null, null);
        }

        public void ReloadThreads()
        {
            if (_downloadTask != null && !_downloadTask.IsCompleted)
            {
                return;
            }

            Threads.Clear();
            ImageThreads.Clear();
            IsError = false;
            IsLoading = true;

            Task<List<Models.Thread>> download = _board.GetThreadsAsync();
            _downloadTask = download.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    IsError = true;
                    IsLoading = false;
                    return;
                }

                IEnumerable<ThreadViewModel> threads = task.Result
                    .Where(x => !x.IsSticky || CriticalSettingsManager.Current.ShowStickies)
                    .Select(x => new ThreadViewModel(x));

                Threads.AddRange(threads);
                ImageThreads.AddRange(threads);

                IsLoading = false;
            }, TaskContinuationOptions.ExecuteSynchronously);
        }

        public void CreateNewThread()
        {
            Threads.IsPaused = true;
            ImageThreads.IsPaused = true;
            Watchlist.IsPaused = true;

            Navigate(new Uri(String.Format("/Views/NewThreadPage.xaml?board={0}", Uri.EscapeUriString(Name)), UriKind.Relative));
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

        private void SearchTextChanged()
        {
            Predicate<ThreadViewModel> filter = (ThreadViewModel tvm) =>
            {
                if (tvm.InitialPost == null) return false;
                return tvm.InitialPost.SimpleComment.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0
                    || (tvm.InitialPost.Subject != null
                        && tvm.InitialPost.Subject.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            };

            Threads.ApplyFilter(filter);
            ImageThreads.ApplyFilter(filter);
            Watchlist.ApplyFilter(filter);
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
                        // We have to get items out of the pending queue, since they
                        // might not of been inserted yet.
                        ThreadViewModel tvm = Watchlist.All().FirstOrDefault(x => x.Number == ((Models.Thread)e.OldItems[0]).Number);
                        if (tvm != null)
                        {
                            Watchlist.RemoveAndPending(tvm);
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
