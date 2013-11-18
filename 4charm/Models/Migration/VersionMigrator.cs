using System.Collections.Generic;
using System.Linq;

namespace _4charm.Models.Migration
{
    /// <summary>
    /// The version migrator handles transferring settings between application versions.
    /// 
    /// Currently the only incompatability is between 1.1 and 1.2, which switched from a
    /// the settings API to using a faster local storage based approach.
    /// </summary>
    public class VersionMigrator
    {
        /// <summary>
        /// Migrate setting from 1.1 to version 1.2.
        /// </summary>
        /// <param name="target"></param>
        public static void Migrate1_1to1_2(Dictionary<string, object> target)
        {
            try
            {
                target["ShowStickies"] = SettingsManager1_1.Current.ShowStickies;
            }
            catch(KeyNotFoundException)
            {
            }

            try
            {
                target["EnableHTTPS"] = SettingsManager1_1.Current.EnableHTTPS;
            }
            catch (KeyNotFoundException)
            {
            }

            try
            {
                target["ShowTripcodes"] = SettingsManager1_1.Current.ShowTripcodes;
            }
            catch (KeyNotFoundException)
            {
            }

            try
            {
                target["LockOrientation"] = SettingsManager1_1.Current.LockOrientation;
            }
            catch (KeyNotFoundException)
            {
            }

            List<string> favorites = SettingsManager1_1.Current.FavoritesSave;
            if (favorites != null)
            {
                target["Favorites"] = favorites;
            }

            List<BoardID> boards = SettingsManager1_1.Current.BoardSave;
            if (boards != null)
            {
                target["Boards"] = new List<string>(boards.Select(x => x.Name));
            }

            // Clear the 1.1 settings afterwards so they never get remigrated.
            SettingsManager1_1.Current.Clear();
        }
    }
}
