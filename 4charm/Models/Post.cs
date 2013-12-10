using _4charm.Models.API;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace _4charm.Models
{
    /// <summary>
    /// Representation of a post on the site. Note that posts are unique, there will never be multiple instances
    /// of "/fa/ - 1111112", etc.
    /// 
    /// Posts should never actually be created, instead you should fetch a desired thread out of the cache using
    /// ThreadCache.EnforceBoard("fa").EnforceBoard(1111111).EnforcePost(132144). This ensures uniqueness and
    /// consistency of post instances across the application.
    /// 
    /// A post contains all pertient information like poster name, comment, subject, image, etc. It also has a link
    /// to the parent thread. Posts are not serialized, cached, or saved in any way between application runs -- EXCEPT
    /// the first post of each thread in the watchlist/history collections, so that these can be displayed infinitely
    /// without refetching the data whenever the application is started.
    /// </summary>
    [DataContract]
    public class Post
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private ulong Number1;

        /// <summary>
        /// Parent thread in the ThreadCache.
        /// </summary>
        public Thread Thread { get; set; }

        /// <summary>
        /// Post ID.
        /// </summary>
        [DataMember]
        public ulong Number { get; set; }

        /// <summary>
        /// If the post is stickied. This is only set on OP posts and really means the thread is stickied.
        /// </summary>
        [DataMember]
        public bool IsSticky { get; set; }

        /// <summary>
        /// If the post is closed. This is only set on OP posts and really means the thread is closed.
        /// </summary>
        [DataMember]
        public bool IsClosed { get; set; }

        /// <summary>
        /// If the post had a file uploaded which was subsequently deleted.
        /// </summary>
        [DataMember]
        public bool FileDeleted { get; set; }

        /// <summary>
        /// The time the post was made.
        /// </summary>
        [DataMember]
        public DateTime Time { get; set; }

        /// <summary>
        /// Poster name.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Post subject.
        /// </summary>
        [DataMember]
        public string Subject { get; set; }

        /// <summary>
        /// Poster tripcode.
        /// </summary>
        [DataMember]
        public string Tripcode { get; set; }

        /// <summary>
        /// Poster capcode.
        /// </summary>
        [DataMember]
        public APIPost.CapCodes CapCode { get; set; }

        /// <summary>
        /// If the poster is a moderator.
        /// </summary>
        public bool IsMod { get { return CapCode == APIPost.CapCodes.mod; } }

        /// <summary>
        /// IF the poster is an administrator.
        /// </summary>
        public bool IsAdmin { get { return CapCode == APIPost.CapCodes.admin || CapCode == APIPost.CapCodes.admin_highlight; } }

        /// <summary>
        /// If the poster is a developer.
        /// </summary>
        public bool IsDeveloper { get { return CapCode == APIPost.CapCodes.developer; } }

        /// <summary>
        /// File name of the post as stored on image servers. This is not the original file name, and is instead used to download
        /// the image.
        /// </summary>
        [DataMember]
        public ulong RenamedFileName { get; set; }

        /// <summary>
        /// Original file name of the image uploaded with this post, if there was one.
        /// </summary>
        [DataMember]
        public string FileName { get; set; }

        /// <summary>
        /// Filetype of the uploaded image, if there is one.
        /// </summary>
        [DataMember]
        public APIPost.FileTypes FileType { get; set; }

        /// <summary>
        /// If the uploaded image is an animated GIF.
        /// </summary>
        public bool IsGIF { get { return FileType == APIPost.FileTypes.gif; } }

        /// <summary>
        /// Post comment.
        /// </summary>
        [DataMember]
        public string Comment { get; set; }
        
        /// <summary>
        /// Number of post replies. This is only set on OP posts, and represents the number of replies to
        /// the thread, excluding the OP.
        /// </summary>
        [DataMember]
        public uint PostCount { get; set; }

        /// <summary>
        /// Number of image replies. This is only set on OP posts, and represents the number of image replies to
        /// the thread, excluding the OP.
        /// </summary>
        [DataMember]
        public uint ImageCount { get; set; }

        /// <summary>
        /// If an image was uploaded, this is the pixel width of that image.
        /// </summary>
        [DataMember]
        public uint ImageWidth { get; set; }

        /// <summary>
        /// If an image was uploaded, this is the pixel height of that image.
        /// </summary>
        [DataMember]
        public uint ImageHeight { get; set; }

        /// <summary>
        /// A list of posts in the same thread, that this post quotes.
        /// </summary>
        public HashSet<ulong> Quotes { get; set; }

        /// <summary>
        /// The height which should be used to display a thumbnail for this post, if the thumbnail width is 100px.
        /// </summary>
        public double ThumbHeight { get { return ImageWidth != 0 ? ImageHeight * (100 / (double)ImageWidth) : 0; } }

        /// <summary>
        /// URI location of the image thumbnail. This will be HTTPS if the HTTPS setting is turned on.
        /// </summary>
        public Uri ThumbnailSrc
        {
            get
            {
                if (CriticalSettingsManager.Current.EnableHTTPS)
                {
                    return new Uri("https://thumbs.4chan.org/" + Thread.Board.Name + "/thumb/" + RenamedFileName + "s.jpg");
                }
                else
                {
                    return new Uri("http://thumbs.4chan.org/" + Thread.Board.Name + "/thumb/" + RenamedFileName + "s.jpg");
                }
            }
        }

        /// <summary>
        /// URI location of the full size image. This will be HTTPS if the HTTPS setting is turned on.
        /// </summary>
        public Uri ImageSrc
        {
            get
            {
                if (CriticalSettingsManager.Current.EnableHTTPS)
                {
                    return new Uri("https://images.4chan.org/" + Thread.Board.Name + "/src/" + RenamedFileName + "." + FileType);
                }
                else
                {
                    return new Uri("http://images.4chan.org/" + Thread.Board.Name + "/src/" + RenamedFileName + "." + FileType);
                }
            }
        }

        /// <summary>
        /// Create a post, given a parent thread and a data set returned by the API.
        /// </summary>
        /// <param name="thread">The parent thread.</param>
        /// <param name="post">Post returned by API.</param>
        public Post(Thread thread, APIPost post)
        {
            Thread = thread;
            Quotes = new HashSet<ulong>();
            Merge(post);
        }

        /// <summary>
        /// We don't serialize the quotes list, since that would be silly.
        /// 
        /// Instead reconstruct it after the post is deserialized.
        /// </summary>
        /// <param name="sc">Ignored.</param>
        [OnDeserialized]
        public void OnDeserialized(StreamingContext sc)
        {
            Quotes = new HashSet<ulong>();
            FindQuotes();
        }

        /// <summary>
        /// Merge a new set of information retrieved from serialization into this post.
        /// </summary>
        /// <param name="post">The post to update this with.</param>
        public void Merge(Post post)
        {
            Number = post.Number;
            IsSticky = post.IsSticky;
            IsClosed = post.IsClosed;
            FileDeleted = post.FileDeleted;
            Time = post.Time;
            Subject = post.Subject;
            Tripcode = post.Tripcode;
            CapCode = post.CapCode;
            RenamedFileName = post.RenamedFileName;
            FileName = post.FileName;
            FileType = post.FileType;
            Comment = post.Comment;
            PostCount = post.PostCount;
            ImageCount = post.ImageCount;
            ImageWidth = post.ImageWidth;
            ImageHeight = post.ImageHeight;

            FindQuotes();
        }

        /// <summary>
        /// Merge a new set of information retrieved from the API into this post.
        /// </summary>
        /// <param name="post">The post to update this with.</param>
        public void Merge(APIPost post)
        {
            Number = post.Number;
            IsSticky = post.IsSticky;
            IsClosed = post.IsClosed;
            FileDeleted = post.FileDeleted;
            Time = UnixEpoch.AddSeconds(post.TimeStamp);
            Subject = post.Subject;
            Tripcode = post.Tripcode;
            CapCode = post.CapCode;
            RenamedFileName = post.RenamedFileName;
            FileName = post.FileName;
            FileType = post.FileType;
            Comment = post.Comment;
            PostCount = post.Replies;
            ImageCount = post.Images;
            ImageWidth = post.ImageWidth;
            ImageHeight = post.ImageHeight;

            // When fetching from the API, we need to translate some stray entities for correct display.
            if (Subject != null)
            {
                Subject = WebUtility.HtmlDecode(Subject).Replace("&#039;", "'").Replace("&#44;", ",");
            }

            // There is an API bug when mods do things wrong and sometimes the post name has an HTML span
            // in it.
            string modName = post.Name;
            if (modName != null && modName.StartsWith("<span"))
            {
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(modName);
                if (CapCode == APIPost.CapCodes.none) CapCode = APIPost.CapCodes.quiet_mod;
                Name = doc.DocumentNode.InnerText;
            }
            else
            {
                Name = post.Name;
            }
            if (Name != null)
            {
                // Names also need to have some stray entities decoded.
                Name = WebUtility.HtmlDecode(Name).Replace("&#039;", "'").Replace("&#44;", ",");
            }

            FindQuotes();
        }

        /// <summary>
        /// Name to display for the poster. This is a combination of the name, tripcode, and capcode, as would be
        /// seen on the website.
        /// </summary>
        public string DisplayName
        {
            get
            {
                string cc = "";
                switch (CapCode)
                {
                    case APIPost.CapCodes.admin:
                    case APIPost.CapCodes.admin_highlight:
                        cc = " ## Admin"; break;
                    case APIPost.CapCodes.developer:
                        cc = " ## Developer"; break;
                    case APIPost.CapCodes.mod:
                        cc = " ## Mod"; break;
                }
                return Name + cc + " " + (CriticalSettingsManager.Current.ShowTripcodes ? Tripcode : "");
            }
        }

        /// <summary>
        /// Update the internal list of posts this post quotes.
        /// </summary>
        private void FindQuotes()
        {
            if (Comment == null) return;

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(Comment);

            var nodes = doc.DocumentNode.Descendants("a");
            foreach (var node in nodes)
            {
                if (node.Name == "a" && node.Attributes.Contains("class") && node.Attributes["class"].Value == "quotelink")
                {
                    Regex r = new Regex("&gt;&gt;(\\d+)");
                    Match m = r.Match(node.InnerText);
                    if (m.Success)
                    {
                        try
                        {
                            Quotes.Add(ulong.Parse(m.Groups[1].Value));
                        }
                        catch (FormatException)
                        {
                            //Probably possible to get malformed quotes that don't fit in a long etc
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Format the post time into a pretty, easily readable display like,
        /// 
        /// 5 minutes ago
        /// 
        /// or
        /// 
        /// yesterday at 3:45 PM
        /// </summary>
        public string PrettyTime
        {
            get
            {
                const int SECOND = 1;
                const int MINUTE = 60 * SECOND;

                var now = DateTime.UtcNow;

                var ts = new TimeSpan(now.Ticks - Time.Ticks);
                double delta = ts.TotalSeconds;

                var realNow = DateTime.Now;
                var real = realNow.Subtract(ts);

                if (delta < 0) return "right now";
                else if (delta < 1 * MINUTE) return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
                else if (delta < 2 * MINUTE) return "a minute ago";
                else if (delta < 60 * MINUTE) return ts.Minutes + " minutes ago";
                else if (delta < 100 * MINUTE) return "about an hour ago";
                else if (realNow.Date == real.Date || delta < 220 * MINUTE) return new TimeSpan(now.AddMinutes(20).Ticks - Time.Ticks).Hours + " hours ago";
                else if (realNow.Date == real.Date.AddDays(1)) return "yesterday at " + real.ToString("t");
                else if (realNow.Date <= real.Date.AddDays(3)) return real.Date.DayOfWeek + " at " + real.ToString("t");
                else if (realNow.Year == real.Year) return real.ToString("M") + " at " + real.ToString("t");
                else return real.ToString("MMMM dd, yyyy") + " at " + real.ToString("t");
            }
        }

        /// <summary>
        /// Utility property, background color of the thread the post is on.
        /// </summary>
        public Brush Background
        {
            get
            {
                return Thread.Board.Brush;
            }
        }

        /// <summary>
        /// Post number prefixed by board display name.
        /// </summary>
        public string LongNumber
        {
            get
            {
                return Thread.Board.DisplayName + " - " + Number;
            }
        }
    }
}
