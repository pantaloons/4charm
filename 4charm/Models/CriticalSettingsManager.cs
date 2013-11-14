using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace _4charm.Models
{
    /// <summary>
    /// Important settings object. This is all settings that *must* be loaded on app startup,
    /// and can't ever be accidentally lost, overwritten, timed out when writing, etc.
    /// 
    /// These are forcibly loaded and block app startup, so don't put garbage in here, or things
    /// which can grow to be unbounded, since when they get large app startup will suffer. The
    /// favorites and all board listings can go here, since those are max ~50 items.
    /// </summary>
    class CriticalSettingsManager : SettingsManager
    {
        /// <summary>
        /// File name for this settings file in isolated storage.
        /// </summary>
        private const string DefaultSettingsFileName = "CriticalSettings.xml";

        /// <summary>
        /// List of non basic types that need to be serialized into this settings file.
        /// </summary>
        private static readonly List<Type> KnownTypes = new List<Type>() { typeof(List<string>), typeof(SupportedPageOrientation) };

        /// <summary>
        /// Singleton critical settings service.
        /// </summary>
        private static CriticalSettingsManager _current = new CriticalSettingsManager();
        public static CriticalSettingsManager Current
        {
            get
            {
                return _current;
            }
        }

        public bool EnableManualRefresh
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4), false); }
            set { SetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4), value); }
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

        public ObservableCollection<Board> Favorites
        {
            get { _rebuildTask.Wait(); return _favorites; }
        }

        public ObservableCollection<Board> Boards
        {
            get { _rebuildTask.Wait(); return _boards; }
        }

        private ObservableCollection<Board> _favorites, _boards;

        /// <summary>
        /// Task tracking progress of collection rebuilding. This gets started off after
        /// the restore completes, and should be waited on before attempting to use
        /// the two rebuilt collections (favorites and all.)
        /// </summary>
        private Task _rebuildTask = null;

        public CriticalSettingsManager()
            : base(DefaultSettingsFileName, KnownTypes)
        {
            // We first "restore" the settings from a file, and then rebuild the history
            // and watchlist collections, which involves fixing up some references that
            // don't get persisted correctly.
            _rebuildTask = Restore().ContinueWith(t => Rebuild(), TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// The serialization for these threads is the minimum required information needed to show them
        /// in the history and watchlists. That is, the board they are from, and the first post, this information
        /// is described by a ThreadID. We represent these as single post threads in the cache, opening the thread
        /// will load the remaining posts again from the net.
        /// </summary>
        private void Rebuild()
        {
            List<string> boards = GetSetting<List<string>>("Boards", BoardList.Boards.Values.Where(x => !x.IsNSFW).Select(x => x.Name).ToList());
            _boards = new SortedObservableCollection<Board>(boards.Where(x => BoardList.Boards.ContainsKey(x))
                .Select(x => ThreadCache.Current.EnforceBoard(x)));

            List<string> favorites = GetSetting<List<string>>("Favorites", new List<string>() { "a", "fa", "fit" });
            _favorites = new ObservableCollection<Board>(favorites.Where(x => BoardList.Boards.ContainsKey(x))
                .Select(x => ThreadCache.Current.EnforceBoard(x)));

            _boards.CollectionChanged += (sender, e) =>
            {
                SetSetting<List<string>>("Boards", _boards.Select(x => x.Name).ToList());
            };

            _favorites.CollectionChanged += (sender, e) =>
            {
                SetSetting<List<string>>("Favorites", _favorites.Select(x => x.Name).ToList());
            };
        }
    }
}
