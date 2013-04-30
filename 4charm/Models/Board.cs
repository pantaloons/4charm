using _4charm.Models.API;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;

namespace _4charm.Models
{
    public class Board
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsNSFW { get; set; }

        public string DisplayName { get { return "/" + Name + "/"; } }
        public bool IsFavorite { get { return SettingsManager.Current.Favorites.Count(x => x.Name == Name) > 0; } }

        public Brush Brush { get { return IsNSFW ? App.Current.Resources["NSFWBrush"] as SolidColorBrush : App.Current.Resources["SFWBrush"] as SolidColorBrush; } }
        public Brush ReplyBackBrush { get { return IsNSFW ? App.Current.Resources["NSFWReplyBackBrush"] as SolidColorBrush : App.Current.Resources["SFWReplyBackBrush"] as SolidColorBrush; } }
        public Brush ReplyForeBrush { get { return IsNSFW ? App.Current.Resources["NSFWReplyForeBrush"] as SolidColorBrush : App.Current.Resources["SFWReplyForeBrush"] as SolidColorBrush; } }
        public Uri IconURI { get { return new Uri(Path.Combine("", "Assets", "Boards", "Icons", Name + ".jpg"), UriKind.Relative); } }
        public Uri WideURI { get { return new Uri(Path.Combine("", "Assets", "Boards", "Fanart", Name + ".jpg"), UriKind.Relative); } }

        public Dictionary<ulong, Thread> Threads { get; set; }

        private object loadLock;

        public Board(string name, string description, bool isNSFW)
        {
            Name = name;
            Description = description;
            IsNSFW = isNSFW;

            Threads = new Dictionary<ulong, Thread>();
            loadLock = new object();
        }

        public void Merge(Board b)
        {
            Name = b.Name;
            Description = b.Description;
            IsNSFW = b.IsNSFW;

            foreach (Thread t in b.Threads.Values)
            {
                EnforceThread(t.Number).Merge(t);
            }
        }

        public Thread EnforceThread(ulong number)
        {
            if (!Threads.ContainsKey(number))
            {
                Threads[number] = new Thread(this, number);
            }
            return Threads[number];
        }

        public async Task<List<Thread>> GetThreadsAsync()
        {
            using (Stream s = await RequestManager.Current.GetStreamAsync(new Uri("http://api.4chan.org/" + Name + "/catalog.json")))
            {
                DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(List<APIPage>));

                List<APIPage> pages = dcjs.ReadObject(s) as List<APIPage>;

                List<Thread> threads = new List<Thread>();
                foreach (APIPage page in pages)
                {
                    foreach (APIPost post in page.Threads)
                    {
                        if (Threads.ContainsKey(post.Number))
                        {
                            Threads[post.Number].Merge(post);
                        }
                        else
                        {
                            Threads[post.Number] = new Thread(this, post);
                        }
                        threads.Add(Threads[post.Number]);
                    }
                }

                return threads;
            }
        }

        public int CompareTo(Board other)
        {
            return Name.CompareTo(other.Name);
        }
    }
}
