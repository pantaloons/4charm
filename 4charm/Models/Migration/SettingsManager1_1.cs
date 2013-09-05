using Microsoft.Phone.Controls;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace _4charm.Models.Migration
{
    /// <summary>
    /// This is a stripped down version of the SettingsManager class used in version 1.1
    /// 
    /// We keep it so that values can be read out of old settings files and transferred into
    /// the newer format.
    /// </summary>
    public class SettingsManager1_1
    {
        /// <summary>
        /// Singleton representing the v1.1 settings manager.
        /// </summary>
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

        /// <summary>
        /// Clear the settings store.
        /// </summary>
        public void Clear()
        {
            IsolatedStorageSettings.ApplicationSettings.Clear();
            IsolatedStorageSettings.ApplicationSettings.Save();
        }

        /// <summary>
        /// Get a setting with a specific name. Throws KeyNotFound if it doesn't exist.
        /// </summary>
        private T GetSetting<T>(string name)
        {
            T val;
            if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<T>(name, out val)) return val;
            else throw new KeyNotFoundException();
        }

        /// <summary>
        /// Show stickies setting.
        /// </summary>
        public bool ShowStickies
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }
        
        /// <summary>
        /// Show tripcodes setting.
        /// </summary>
        public bool ShowTripcodes
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        /// <summary>
        /// Enable HTTPS setting.
        /// </summary>
        public bool EnableHTTPS
        {
            get { return GetSetting<bool>(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        /// <summary>
        /// Orientation lock.
        /// </summary>
        public SupportedPageOrientation LockOrientation
        {
            get { return GetSetting<SupportedPageOrientation>(MethodBase.GetCurrentMethod().Name.Substring(4)); }
        }

        /// <summary>
        /// Favorites.
        /// </summary>
        public List<string> FavoritesSave
        {
            get
            {
                List<string> value;
                if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<List<string>>("FavoritesSave", out value)) return value;
                return null;
            }
        }

        /// <summary>
        /// List of all boards.
        /// </summary>
        public List<BoardID> BoardSave
        {
            get
            {
                List<BoardID> value;
                if (IsolatedStorageSettings.ApplicationSettings.TryGetValue<List<BoardID>>("BoardSave", out value)) return value;
                return null;
            }
        }

        /// <summary>
        /// Private singleton constructor.
        /// </summary>
        private SettingsManager1_1()
        {
        }
    }
}
