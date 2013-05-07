using _4charm.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4charm.Models
{
    class TransitorySettingsManager : SettingsManager
    {
        private const string DefaultSettingsFileName = "TransitorySettings.xml";
        private static readonly List<Type> KnownTypes = new List<Type>() { typeof(List<ThreadID>) };

        private static TransitorySettingsManager _current = new TransitorySettingsManager();
        public static TransitorySettingsManager Current
        {
            get
            {
                return _current;
            }
        }

        private ObservableCollection<ThreadViewModel> _watchlist, _history;
        private Task _rebuildTask = null;

        public TransitorySettingsManager()
            : base(DefaultSettingsFileName, KnownTypes)
        {
            _rebuildTask = Restore().ContinueWith(t => Rebuild(), TaskScheduler.Current);
        }

        public ObservableCollection<ThreadViewModel> History
        {
            get { Restore(); _rebuildTask.Wait(); return _history; }
        }

        public ObservableCollection<ThreadViewModel> Watchlist
        {
            get { Restore(); _rebuildTask.Wait(); return _watchlist; }
        }

        private void Rebuild()
        {
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
