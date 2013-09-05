using _4charm.Models.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows.Media;

namespace _4charm.Models
{
    /// <summary>
    /// Representation of a board on the site. Note that boards are unique, there will never be multiple instances
    /// of "/fa/", etc.
    /// 
    /// Boards should never actually be created, instead you should fetch a desired board out of the cache using
    /// ThreadCache.EnforceBoard("fa"). This ensures uniqueness and consistency of object instances across the application.
    /// 
    /// A board contains a list of threads, each with a list of posts, and some minor metadata about the board. Boards are not
    /// serialized, cached, or saved in any way between application runs.
    /// </summary>
    public class Board
    {
        /// <summary>
        /// Board name, like "fa". This is unique and serves as the board identifier for the cache.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Board description, like "Fashion". This is not unique, and is a purely decorative/visual field.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// If the board is NSFW. This helps in default "all" list population, and determines the background color
        /// of posts on this board.
        /// </summary>
        public bool IsNSFW { get; set; }

        /// <summary>
        /// Utility field for the name wrapper in slahses, like "/fa/".
        /// </summary>
        public string DisplayName { get { return "/" + Name + "/"; } }

        /// <summary>
        /// If the board is favorited. This does not dynamically update, and callers need to refetch it when they suspect
        /// it changed.
        /// </summary>
        public bool IsFavorite { get { return CriticalSettingsManager.Current.Favorites.Count(x => x.Name == Name) > 0; } }

        /// <summary>
        /// Background brush color for the board. Blue if SFW, pink for NSFW.
        /// </summary>
        public Brush Brush { get { return IsNSFW ? App.Current.Resources["NSFWBrush"] as SolidColorBrush : App.Current.Resources["SFWBrush"] as SolidColorBrush; } }

        /// <summary>
        /// Background color for field descriptions in the inline reply area of posts page.
        /// </summary>
        public Brush ReplyBackBrush { get { return IsNSFW ? App.Current.Resources["NSFWReplyBackBrush"] as SolidColorBrush : App.Current.Resources["SFWReplyBackBrush"] as SolidColorBrush; } }

        /// <summary>
        /// Text color for field descriptions in the inline reply area of posts page.
        /// </summary>
        public Brush ReplyForeBrush { get { return IsNSFW ? App.Current.Resources["NSFWReplyForeBrush"] as SolidColorBrush : App.Current.Resources["SFWReplyForeBrush"] as SolidColorBrush; } }

        /// <summary>
        /// Square icon for use on home screen tiles.
        /// </summary>
        public Uri IconURI { get { return new Uri(Path.Combine("", "Assets", "Boards", "Icons", Name + ".jpg"), UriKind.Relative); } }

        /// <summary>
        /// Wide icon for use on favorites and all board listings, as well as wide home screen tile.
        /// </summary>
        public Uri WideURI { get { return new Uri(Path.Combine("", "Assets", "Boards", "Fanart", Name + ".jpg"), UriKind.Relative); } }

        /// <summary>
        /// List of threads on the board. This does not represent the current view, and threads do not get
        /// removed while the application is running, so information continuously accumulates in this dictionary.
        /// 
        /// This gets thrown out when the application is terminated.
        /// 
        /// If new information is available to overwrite existing thread information, it should be "Merged"
        /// in with the appropriate Merge API.
        /// </summary>
        public Dictionary<ulong, Thread> Threads { get; set; }

        /// <summary>
        /// Construct a new board object. Should only be called by the ThreadCache.
        /// </summary>
        /// <param name="name">Name of the board.</param>
        /// <param name="description">Description for the board.</param>
        /// <param name="isNSFW">If the board is not safe for work.</param>
        public Board(string name, string description, bool isNSFW)
        {
            Name = name;
            Description = description;
            IsNSFW = isNSFW;

            Threads = new Dictionary<ulong, Thread>();
        }

        /// <summary>
        /// Ensures that a thread exists in the cache, by creating it if it does not. The thread
        /// does not need to have any information except an identifier (the initial post number).
        /// </summary>
        /// <param name="number">The ID of the thread</param>
        /// <returns>The instance of that thread in the cache. May be newly created.</returns>
        public Thread EnforceThread(ulong number)
        {
            if (!Threads.ContainsKey(number))
            {
                Threads[number] = new Thread(this, number);
            }
            return Threads[number];
        }

        /// <summary>
        /// Get a list of threads currently on the board. This can throw a whole lot of garbage for network, parse,
        /// and other exceptions.
        /// </summary>
        /// <returns>The list of threads on the board, in bump order.</returns>
        public async Task<List<Thread>> GetThreadsAsync()
        {
            // The catalog API is the best for the threads view, since it retrieves the entire board in one go.
            // The downside is that we only get one post for each thread, not three, and so the fade in animations
            // can sometimes stall while data load occurs on each individual thread, but I think this is a good
            // tradeoff.
            using (Stream s = await RequestManager.Current.GetStreamAsync(new Uri("http://api.4chan.org/" + Name + "/catalog.json")))
            {
                DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(List<APIPage>));

                List<APIPage> pages = await dcjs.ReadObjectAsync<List<APIPage>>(s);

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

        /// <summary>
        /// Comparator for default board sort order in the "all" listing.
        /// </summary>
        public int CompareTo(Board other)
        {
            return Name.CompareTo(other.Name);
        }
    }
}
