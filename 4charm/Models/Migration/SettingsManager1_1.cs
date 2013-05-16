using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace _4charm.Models.Migration
{
    public class SettingsManager1_1
    {
        private static SettingsManager1_1 _current;
        public static SettingsManager1_1 Current
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_current == null) _current = new SettingsManager1_1();
                return _current;
            }
        }

        public void Clear()
        {
            IsolatedStorageSettings.ApplicationSettings.Clear();
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        private Dictionary<string, object> loaded = new Dictionary<string, object>();
        private T GetSetting<T>(string name)
        {
            T val;
            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<T>(name, out val)) return val;
            else throw new KeyNotFoundException();
        }

        public bool ShowStickies
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        public bool ShowTripcodes
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        public bool EnableHTTPS
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        public SupportedPageOrientation LockOrientation
        {
            get { return GetSetting<SupportedPageOrientation>(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        public List<string> FavoritesSave
        {
            get
            {
                List<string> value;
                if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<List<string>>("FavoritesSave", out value)) return value;
                return null;
            }
        }

        public List<BoardID> BoardSave
        {
            get
            {
                List<BoardID> value;
                if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<List<BoardID>>("BoardSave", out value)) return value;
                return null;
            }
        }

        private SettingsManager1_1()
        {
        }
    }
}
