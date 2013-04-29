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
    [DataContract]
    public class Post : IComparable<Post>
    {
        public Thread Thread { get; set; }

        [DataMember]
        public ulong Number { get; set; }
        [DataMember]
        public bool IsSticky { get; set; }
        [DataMember]
        public bool IsClosed { get; set; }
        [DataMember]
        public bool FileDeleted { get; set; }
        [DataMember]
        public DateTime Time { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Subject { get; set; }
        [DataMember]
        public string Tripcode { get; set; }
        [DataMember]
        public APIPost.CapCodes CapCode { get; set; }
        public bool IsMod { get { return CapCode == APIPost.CapCodes.mod; } }
        public bool IsAdmin { get { return CapCode == APIPost.CapCodes.admin || CapCode == APIPost.CapCodes.admin_highlight; } }
        public bool IsDeveloper { get { return CapCode == APIPost.CapCodes.developer; } }
        [DataMember]
        public ulong RenamedFileName { get; set; }
        [DataMember]
        public string FileName { get; set; }
        [DataMember]
        public APIPost.FileTypes FileType { get; set; }
        public bool IsGIF { get { return FileType == APIPost.FileTypes.gif; } }
        [DataMember]
        public string Comment { get; set; }
        [DataMember]
        public uint PostCount { get; set; }
        [DataMember]
        public uint ImageCount { get; set; }
        [DataMember]
        public uint ImageWidth { get; set; }
        [DataMember]
        public uint ImageHeight { get; set; }

        public HashSet<ulong> Quotes { get; set; }

        public double ThumbHeight { get { return ImageWidth != 0 ? ImageHeight * (100 / (double)ImageWidth) : 0; } }

        public Uri ThumbnailSrc
        {
            get
            {
                if (SettingsManager.Current.EnableHTTPS)
                {
                    return new Uri("https://thumbs.4chan.org/" + Thread.Board.Name + "/thumb/" + RenamedFileName + "s.jpg");
                }
                else
                {
                    return new Uri("http://thumbs.4chan.org/" + Thread.Board.Name + "/thumb/" + RenamedFileName + "s.jpg");
                }
            }
        }

        public Uri ImageSrc
        {
            get
            {
                if (SettingsManager.Current.EnableHTTPS)
                {
                    return new Uri("https://images.4chan.org/" + Thread.Board.Name + "/src/" + RenamedFileName + "." + FileType);
                }
                else
                {
                    return new Uri("http://images.4chan.org/" + Thread.Board.Name + "/src/" + RenamedFileName + "." + FileType);
                }
            }
        }

        public Post(Thread thread, APIPost post)
        {
            Thread = thread;
            Quotes = new HashSet<ulong>();
            Merge(post);
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext sc)
        {
            Quotes = new HashSet<ulong>();
            FindQuotes();
        }

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

        public void Merge(APIPost post)
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
            PostCount = post.Replies;
            ImageCount = post.Images;
            ImageWidth = post.ImageWidth;
            ImageHeight = post.ImageHeight;

            if (Subject != null)
            {
                Subject = WebUtility.HtmlDecode(Subject).Replace("&#039;", "'").Replace("&#44;", ",");
            }

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
                Name = WebUtility.HtmlDecode(Name).Replace("&#039;", "'").Replace("&#44;", ",");
            }

            FindQuotes();
        }

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
                return Name + cc + " " + (SettingsManager.Current.ShowTripcodes ? Tripcode : "");
            }
        }

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
                    else
                    {
                    }
                }
            }
        }

        public string PrettyTime
        {
            get
            {
                const int SECOND = 1;
                const int MINUTE = 60 * SECOND;

                var now = DateTime.UtcNow;
                var post = Time.AddHours(4);

                var ts = new TimeSpan(now.Ticks - post.Ticks);
                double delta = ts.TotalSeconds;

                if (delta < 0) return "right now";
                else if (delta < 1 * MINUTE) return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
                else if (delta < 2 * MINUTE) return "a minute ago";
                else if (delta < 60 * MINUTE) return ts.Minutes + " minutes ago";
                else if (delta < 100 * MINUTE) return "about an hour ago";
                else if (now.Date == post.Date) return new TimeSpan(now.AddMinutes(20).Ticks - post.Ticks).Hours + " hours ago";
                else if (now.Date == post.Date.AddDays(1)) return "yesterday at " + post.ToString("hh:mm");
                else if (now.Date <= post.Date.AddDays(3)) return now.Date.DayOfWeek + " at " + post.ToString("hh:mm");
                else if (now.Year == post.Year) return post.ToString("dd MMMM") + " at " + post.ToString("hh:mm");
                else return post.ToString("dd MMMM yyyy") + " at " + post.ToString("hh:mm");
            }
        }

        public Brush Background
        {
            get
            {
                return Thread.Board.Brush;
            }
        }

        public int CompareTo(Post other)
        {
            if (Thread.Board.Name == other.Thread.Board.Name) return Number.CompareTo(other.Number);
            else return Thread.Board.Name.CompareTo(other.Thread.Board.Name);
        }
    }
}
