using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace _4charm.Models
{
    class SettingsManager
    {
        private static SettingsManager _current;
        public static SettingsManager Current
        {
            get
            {
                if (_current == null)
                {
                    _current = new SettingsManager();
                }
                return _current;
            }
        }

        private T GetSetting<T>(string name, T first)
        {
            Restore().Wait();

            if (_sessionState.ContainsKey(name) && _sessionState[name] is T) return (T)_sessionState[name];
            else return (T)first;
        }

        private void SetSetting<T>(string name, T value)
        {
            Restore().Wait();

            _sessionState[name] = value;

            Save();
        }

        public bool ShowStickies
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4), true); }
            set { SetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4), value); }
        }

        public bool ShowTripcodes
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4), true); }
            set { SetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4), value); }
        }

        public bool EnableHTTPS
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4), false); }
            set { SetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4), value); }
        }

        public SupportedPageOrientation LockOrientation
        {
            get { return GetSetting<SupportedPageOrientation>(MethodBase.GetCurrentMethod().Name.Substring(4), SupportedPageOrientation.PortraitOrLandscape); }
            set { SetSetting<SupportedPageOrientation>(MethodBase.GetCurrentMethod().Name.Substring(4), value); }
        }

        public ObservableCollection<BoardViewModel> Favorites
        {
            get { Restore(); _rebuildTask.Wait(); return _favorites; }
        }
        public ObservableCollection<BoardViewModel> Boards
        {
            get { Restore(); _rebuildTask.Wait(); return _boards; }
        }
        public ObservableCollection<ThreadViewModel> History
        {
            get { Restore(); _rebuildTask.Wait(); return _history; }
        }
        public ObservableCollection<ThreadViewModel> Watchlist
        {
            get { Restore(); _rebuildTask.Wait(); return _watchlist; }
        }

        private Dictionary<string, object> _sessionState = new Dictionary<string, object>();
        private const string sessionStateFilename = "_sessionState.xml";
        private Task _restoreTask = null, _rebuildTask = null;
        private Task<Task> _saveTask = null;
        private Task _queuedTask = null;
        private List<Type> _knownTypes = new List<Type>() { typeof(List<string>), typeof(List<ThreadID>), typeof(SupportedPageOrientation) };

        private ObservableCollection<BoardViewModel> _favorites, _boards;
        private ObservableCollection<ThreadViewModel> _watchlist, _history;

        private SettingsManager()
        {
            Restore();
        }

        /// <summary>
        /// Save the current <see cref="SessionState"/>.  Any <see cref="Frame"/> instances
        /// registered with <see cref="RegisterFrame"/> will also preserve their current
        /// navigation stack, which in turn gives their active <see cref="Page"/> an opportunity
        /// to save its state.
        /// </summary>
        /// <returns>An asynchronous task that reflects when session state has been saved.</returns>
        private Task Save()
        {
            if (_saveTask == null)
            {
                _saveTask = new Task<Task>(SaveAsyncImpl);
                _saveTask.Start(TaskScheduler.FromCurrentSynchronizationContext());
            }
            else if (_queuedTask == null || (_queuedTask.Status != TaskStatus.WaitingForActivation &&
                                             _queuedTask.Status != TaskStatus.WaitingForChildrenToComplete &&
                                             _queuedTask.Status != TaskStatus.WaitingToRun))
            {
                _queuedTask = new Task<Task>(SaveAsyncImpl);
                _saveTask = _saveTask.Unwrap().ContinueWith(t => _queuedTask, TaskScheduler.FromCurrentSynchronizationContext());
            }
            else
            {
                // The _saveTask is running, and there is a continuation _queuedTask which has not yet started execution.
                // The continuation will also save whatever changes generated this call to save, so we can safely drop the
                // call.
            }
            return _saveTask;
        }

        private async Task SaveAsyncImpl()
        {
            try
            {
                // Serialize the session state synchronously to avoid asynchronous access to shared
                // state
                MemoryStream sessionData = new MemoryStream();
                DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                serializer.WriteObject(sessionData, _sessionState);

                // Get an output stream for the SessionState file and write the state asynchronously
                StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(sessionStateFilename, CreationCollisionOption.ReplaceExisting);
                using (Stream fileStream = await file.OpenStreamForWriteAsync())
                {
                    sessionData.Seek(0, SeekOrigin.Begin);
                    await sessionData.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
            catch
            {
                // Do nothing
            }
        }

        /// <summary>
        /// Restores previously saved <see cref="SessionState"/>.  Any <see cref="Frame"/> instances
        /// registered with <see cref="RegisterFrame"/> will also restore their prior navigation
        /// state, which in turn gives their active <see cref="Page"/> an opportunity restore its
        /// state.
        /// </summary>
        /// <returns>An asynchronous task that reflects when session state has been read.  The
        /// content of <see cref="SessionState"/> should not be relied upon until this task
        /// completes.</returns>
        private Task Restore()
        {
            if (_restoreTask == null)
            {
                _restoreTask = Task.Run(async () => await RestoreAsyncImpl());
                _rebuildTask = _restoreTask.ContinueWith(t => Rebuild(), TaskScheduler.Current);
            }
            return _restoreTask;
        }

        private async Task RestoreAsyncImpl()
        {
            _sessionState = new Dictionary<String, Object>();

            try
            {
                // Get the input stream for the SessionState file
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(sessionStateFilename);
                using (IInputStream inStream = await file.OpenSequentialReadAsync())
                {
                    // Deserialize the Session State
                    DataContractSerializer serializer = new DataContractSerializer(typeof(Dictionary<string, object>), _knownTypes);
                    _sessionState = (Dictionary<string, object>)serializer.ReadObject(inStream.AsStreamForRead());
                }
            }
            catch
            {
            }
        }

        private void Rebuild()
        {
            List<string> boards = GetSetting<List<string>>("Boards", BoardList.Boards.Values.Where(x => !x.IsNSFW || x.IsNSFW).Select(x => x.Name).ToList());
            _boards = new SortedObservableCollection<BoardViewModel>(boards.Where(x => BoardList.Boards.ContainsKey(x))
                .Select(x => new BoardViewModel(ThreadCache.Current.EnforceBoard(x))));

            List<string> favorites = GetSetting<List<string>>("Favorites", new List<string>() { "a", "fa", "fit" });
            _favorites = new ObservableCollection<BoardViewModel>(favorites.Where(x => BoardList.Boards.ContainsKey(x))
                .Select(x => new BoardViewModel(ThreadCache.Current.EnforceBoard(x))));

            List<ThreadID> watchlist = GetSetting<List<ThreadID>>("Watchlist", new List<ThreadID>());
            _watchlist = new ObservableCollection<ThreadViewModel>(watchlist.Where(x => BoardList.Boards.ContainsKey(x.BoardName))
                .Select(x =>
                {
                    Thread t = ThreadCache.Current.EnforceBoard(x.BoardName).EnforceThread(x.Number);
                    x.Initial.Thread = t;
                    t.Merge(x.Initial);
                    return new ThreadViewModel(t);
                }));

            List<ThreadID> history = GetSetting<List<ThreadID>>("History", new List<ThreadID>());
            _history = new ObservableCollection<ThreadViewModel>(history.Where(x => BoardList.Boards.ContainsKey(x.BoardName))
                .Select(x =>
                {
                    Thread t = ThreadCache.Current.EnforceBoard(x.BoardName).EnforceThread(x.Number);
                    x.Initial.Thread = t;
                    t.Merge(x.Initial);
                    return new ThreadViewModel(t);
                }));

            _boards.CollectionChanged += (sender, e) =>
            {
                SetSetting<List<string>>("Boards", _boards.Select(x => x.Name).ToList());
            };

            _favorites.CollectionChanged += (sender, e) =>
            {
                SetSetting<List<string>>("Favorites", _favorites.Select(x => x.Name).ToList());
            };

            _watchlist.CollectionChanged += (sender, e) =>
            {
                SetSetting<List<ThreadID>>("Watchlist", _watchlist.Select(x => new ThreadID(x.BoardName, x.Number, x.InitialPost._post)).ToList());
            };

            _history.CollectionChanged += (sender, e) =>
            {
                SetSetting<List<ThreadID>>("History", _history.Select(x => new ThreadID(x.BoardName, x.Number, x.InitialPost._post)).Take(50).ToList());
            };
        }
    }
}
