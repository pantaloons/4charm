using _4charm.ViewModels;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4charm.Models.Migration
{
    public class VersionMigrator
    {
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
        }
    }
}
