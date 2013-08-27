using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace _4charm.Models
{
    class CriticalSettingsManager : SettingsManager
    {
        private const string DefaultSettingsFileName = "CriticalSettings.xml";
        private static readonly List<Type> KnownTypes = new List<Type>() { typeof(List<string>), typeof(SupportedPageOrientation) };

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

        public ObservableCollection<BoardViewModel> Favorites
        {
            get { Restore(); _rebuildTask.Wait(); return _favorites; }
        }

        public ObservableCollection<BoardViewModel> Boards
        {
            get { Restore(); _rebuildTask.Wait(); return _boards; }
        }

        private ObservableCollection<BoardViewModel> _favorites, _boards;
        private Task _rebuildTask = null;

        public CriticalSettingsManager()
            : base(DefaultSettingsFileName, KnownTypes)
        {
            _rebuildTask = Restore().ContinueWith(t => Rebuild(), TaskScheduler.Current);
        }

        private void Rebuild()
        {
            List<string> boards = GetSetting<List<string>>("Boards", BoardList.Boards.Values.Where(x => !x.IsNSFW).Select(x => x.Name).ToList());
            _boards = new SortedObservableCollection<BoardViewModel>(boards.Where(x => BoardList.Boards.ContainsKey(x))
                .Select(x => new BoardViewModel(ThreadCache.Current.EnforceBoard(x))));

            List<string> favorites = GetSetting<List<string>>("Favorites", new List<string>() { "a", "fa", "fit" });
            _favorites = new ObservableCollection<BoardViewModel>(favorites.Where(x => BoardList.Boards.ContainsKey(x))
                .Select(x => new BoardViewModel(ThreadCache.Current.EnforceBoard(x))));

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
