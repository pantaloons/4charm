using _4charm.Models.API;
using _4charm.Resources;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
    public class Board : IComparable<Board>
    {
        /// <summary>
        /// Types of tiles pinnable for a board.
        /// </summary>
        public enum TileType
        {
            Text,
            Image
        };

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
        /// Square icon for use on home screen tiles.
        /// </summary>
        public Uri IconURI { get { return new Uri(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "Boards", "Icons", Name + ".jpg"), UriKind.Absolute); } }
        private Uri PinIconURI { get { return new Uri(Path.Combine("", "Assets", "Boards", "Icons", Name + ".jpg"), UriKind.Relative); } } 

        /// <summary>
        /// Wide icon for use on favorites and all board listings, as well as wide home screen tile.
        /// </summary>
        public Uri WideURI { get { return new Uri(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "Assets", "Boards", "Fanart", Name + ".jpg"), UriKind.Absolute); } }
        private Uri PinWideURI { get { return new Uri(Path.Combine("", "Assets", "Boards", "Fanart", Name + ".jpg"), UriKind.Relative); } }

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
            using (Stream s = await RequestManager.Current.GetStreamAsync(new Uri("http://a.4cdn.org/" + Name + "/catalog.json")))
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
        /// Pin tile to start screen with a given tile type.
        /// </summary>
        /// <param name="type">The type of tile to pin.</param>
        public void PinToStart(TileType type)
        {
            switch (type)
            {
                case TileType.Image:
                    PinImageToStart();
                    break;
                case TileType.Text:
                    PinTextToStart();
                    break;
            }
        }

        private void PinImageToStart()
        {
            Uri pin = new Uri("/Views/ThreadsPage.xaml?board=" + Name, UriKind.Relative);
            FlipTileData data = new FlipTileData()
            {
                BackgroundImage = PinIconURI,
                WideBackgroundImage = PinWideURI,
                Title = DisplayName
            };
            try
            {
                ShellTile.Create(pin, data, true);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(AppResources.BoardsPage_AlreadyPinned);
            }
        }

        private void PinTextToStart()
        {
            Grid grid = new Grid();
            grid.Background = (Brush)App.Current.Resources["TransparentBrush"];

            TextBlock heading = new TextBlock();
            heading.FontFamily = new FontFamily("Segoe WP Bold");
            heading.Foreground = new SolidColorBrush(Colors.White);
            heading.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            heading.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            heading.Text = DisplayName;

            grid.Children.Add(heading);

            heading.FontSize = 64 - 10 * (Name.Length - 1);
            heading.Margin = new Thickness(0, -16 + 4 * (Name.Length - 1), 0, 0);
            WriteableBitmap small = new WriteableBitmap(159, 159);
            grid.UpdateLayout();
            grid.Measure(new Size(159, 159));
            grid.Arrange(new Rect(0, 0, 159, 159));
            small.Render(grid, null);
            small.Invalidate();
            CompensateForRender(small.Pixels);

            heading.FontSize = 126 - 20 * (Name.Length - 1);
            heading.Margin = new Thickness(0, -24 + 5 * (Name.Length - 1), 0, 0);
            WriteableBitmap large = new WriteableBitmap(336, 336);
            grid.UpdateLayout();
            grid.Measure(new Size(336, 336));
            grid.Arrange(new Rect(0, 0, 336, 336));
            large.Render(grid, null);
            large.Invalidate();
            CompensateForRender(large.Pixels);

            heading.FontSize = 126;
            heading.Margin = new Thickness(0, -24, 0, 0);
            WriteableBitmap wide = new WriteableBitmap(691, 336);
            grid.UpdateLayout();
            grid.Measure(new Size(691, 336));
            grid.Arrange(new Rect(0, 0, 691, 336));
            wide.Render(grid, null);
            wide.Invalidate();
            CompensateForRender(wide.Pixels);

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("/Shared/ShellContent/tile-" + Name + "-small.png", FileMode.Create, isf))
                {
                    small.WritePNG(isfs);
                }
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("/Shared/ShellContent/tile-" + Name + "-large.png", FileMode.Create, isf))
                {
                    large.WritePNG(isfs);
                }
                using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("/Shared/ShellContent/tile-" + Name + "-wide.png", FileMode.Create, isf))
                {
                    wide.WritePNG(isfs);
                }
            }

            Uri pin = new Uri("/Views/ThreadsPage.xaml?board=" + Name, UriKind.Relative);
            FlipTileData data = new FlipTileData()
            {
                SmallBackgroundImage = new Uri("isostore:/Shared/ShellContent/tile-" + Name + "-small.png", UriKind.Absolute),
                BackgroundImage = new Uri("isostore:/Shared/ShellContent/tile-" + Name + "-large.png", UriKind.Absolute),
                WideBackgroundImage = new Uri("isostore:/Shared/ShellContent/tile-" + Name + "-wide.png", UriKind.Absolute),
            };
            try
            {
                ShellTile.Create(pin, data, true);
            }
            catch (InvalidOperationException)
            {
                MessageBox.Show(AppResources.BoardsPage_AlreadyPinned);
            }
        }

        /// <summary>
        /// Compensate for bad BitmapImage transparency.
        /// </summary>
        /// <param name="bitmapPixels">Pixel array to modify.</param>
        private void CompensateForRender(int[] bitmapPixels)
        {
            if (bitmapPixels.Length == 0) return;

            for (int i = 0; i < bitmapPixels.Length; i++)
            {
                uint pixel = unchecked((uint)bitmapPixels[i]);

                double a = (pixel >> 24) & 255;
                if ((a == 255) || (a == 0)) continue;

                double r = (pixel >> 16) & 255;
                double g = (pixel >> 8) & 255;
                double b = (pixel) & 255;

                double factor = 255 / a;
                uint newR = (uint)Math.Round(r * factor);
                uint newG = (uint)Math.Round(g * factor);
                uint newB = (uint)Math.Round(b * factor);

                // compose
                bitmapPixels[i] = unchecked((int)((pixel & 0xFF000000) | (newR << 16) | (newG << 8) | newB));
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
