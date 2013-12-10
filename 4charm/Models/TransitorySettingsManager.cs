using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace _4charm.Models
{
    /// <summary>
    /// Unimportant settings object. This currently includes the watchlist and history.
    /// 
    /// These are loaded separately from the rest of the settings because they have unbounded size,
    /// take significantly longer to load, and would delay app startup when we only need a few settings
    /// to start (orientation lock).
    /// </summary>
    class TransitorySettingsManager : SettingsManager
    {
        /// <summary>
        /// Maximum number of history entries to keep before purging least recently used.
        /// </summary>
        private const int MaxHistoryEntries = 50;

        /// <summary>
        /// File name for this settings file in isolated storage.
        /// </summary>
        private const string DefaultSettingsFileName = "TransitorySettings.xml";

        /// <summary>
        /// List of non basic types that need to be serialized into this settings file.
        /// </summary>
        private static readonly List<Type> KnownTypes = new List<Type>() { typeof(List<ThreadID>) };

        /// <summary>
        /// Singleton transitory settings service.
        /// </summary>
        private static TransitorySettingsManager _current = new TransitorySettingsManager();
        public static TransitorySettingsManager Current
        {
            get
            {
                return _current;
            }
        }

        /// <summary>
        /// Storage for friendly watchlist and history objects.
        /// </summary>
        private ObservableCollection<Thread> _watchlist, _history;

        /// <summary>
        /// If the settings have been rebuilt yet.
        /// </summary>
        private bool _isRebuilt;

        public TransitorySettingsManager()
            : base(DefaultSettingsFileName, KnownTypes)
        {
            // We first "restore" the settings from a file, and then rebuild the history
            // and watchlist collections, which involves fixing up some references that
            // don't get persisted correctly. Those collections should wait on the rebuild
            // task before returning.
            Restore();
        }

        /// <summary>
        /// Friendly view of the history collection. The settings class registers to changes on this collection
        /// and seamlessly updates the saved values in the background, so modify it as desired.
        /// </summary>
        public ObservableCollection<Thread> History
        {
            get { Rebuild(); return _history; }
        }

        /// <summary>
        /// Friendly view of the watchlist collection. The settings class registers to changes on this collection
        /// and seamlessly updates the saved values in the background, so modify it as desired.
        /// </summary>
        public ObservableCollection<Thread> Watchlist
        {
            get { Rebuild(); return _watchlist; }
        }

        /// <summary>
        /// The serialization for these threads is the minimum required information needed to show them
        /// in the history and watchlists. That is, the board they are from, and the first post, this information
        /// is described by a ThreadID. We represent these as single post threads in the cache, opening the thread
        /// will load the remaining posts again from the net.
        /// </summary>
        private void Rebuild()
        {
            if (_isRebuilt)
            {
                return;
            }

            _isRebuilt = true;
            Restore().Wait();

            // Translate the serialized watchlist into valid usable objects.
            List<ThreadID> watchlist = GetSetting<List<ThreadID>>("Watchlist", new List<ThreadID>());
            _watchlist = new ObservableCollection<Thread>(watchlist.Where(x => BoardList.Boards.ContainsKey(x.BoardName))
                .Select(x =>
                {
                    Thread t = ThreadCache.Current.EnforceBoard(x.BoardName).EnforceThread(x.Number);
                    if (x.Initial != null)
                    {
                        x.Initial.Thread = t;
                        t.Merge(x.Initial);
                    }
                    return t;
                }));

            // Translate the history into valid usable objects.
            List<ThreadID> history = GetSetting<List<ThreadID>>("History", new List<ThreadID>());
            _history = new ObservableCollection<Thread>(history.Where(x => BoardList.Boards.ContainsKey(x.BoardName))
                .Select(x =>
                {
                    Thread t = ThreadCache.Current.EnforceBoard(x.BoardName).EnforceThread(x.Number);
                    if (x.Initial != null)
                    {
                        x.Initial.Thread = t;
                        t.Merge(x.Initial);
                    }
                    return t;
                }));

            // Wire to collection changed events to trigger reserialization. We can just do this as often as we like, since the settings manager
            // queueing ensures it won't do redudnant save work.
            _watchlist.CollectionChanged += (sender, e) =>
            {
                SetSetting<List<ThreadID>>("Watchlist", _watchlist.Select(x => new ThreadID(x.Board.Name, x.Number, x.Posts.FirstOrDefault().Value)).ToList());
            };

            _history.CollectionChanged += (sender, e) =>
            {
                SetSetting<List<ThreadID>>("History", _history.Select(x => new ThreadID(x.Board.Name, x.Number, x.Posts.FirstOrDefault().Value)).Take(MaxHistoryEntries).ToList());
            };
        }
    }
}
