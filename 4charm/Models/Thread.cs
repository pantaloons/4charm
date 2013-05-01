using _4charm.Models.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Windows;

namespace _4charm.Models
{
    public class Thread : IComparable<Thread>
    {
        public Board Board { get; set; }

        public ulong Number { get; set; }

        public Post Initial { get { return Posts.FirstOrDefault().Value; } }
        public bool IsSticky { get { return Initial != null ? Initial.IsSticky : false; } }
        public string Subject { get { return Initial != null ? Initial.Subject : null; } }

        public ulong ImageCount { get { return Initial != null ? Initial.ImageCount : 0; } }
        
        public SortedDictionary<ulong, Post> Posts { get; set; }

        private object loadLock = new object();

        public Thread(Board board, APIPost op)
        {
            Board = board;
            Posts = new SortedDictionary<ulong, Post>();
            Merge(op);
        }

        public Thread(Board board, ulong number)
        {
            Board = board;
            Posts = new SortedDictionary<ulong, Post>();
            Number = number;
        }

        public void Merge(Thread t)
        {
            Number = t.Number;

            foreach (Post p in t.Posts.Values)
            {
                if (Posts.ContainsKey(p.Number)) Posts[p.Number].Merge(p);
                else Posts[p.Number] = p;
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                //NotifyPropertiesChanged();
            });
        }

        public void Merge(Post op)
        {
            if (Posts.ContainsKey(op.Number))
            {
                Posts[op.Number].Merge(op);
            }
            else
            {
                Posts[op.Number] = op;
            }
        }

        public void Merge(APIPost op)
        {
            Number = op.Number;
            UpdatePost(op);
        }

        private void UpdatePost(APIPost p)
        {
            if (Posts.ContainsKey(p.Number))
            {
                Posts[p.Number].Merge(p);
            }
            else
            {
                Posts[p.Number] = new Post(this, p);
            }
        }

        public async Task<List<Post>> GetPostsAsync()
        {
            using (Stream s = await RequestManager.Current.GetStreamAsync(new Uri("http://api.4chan.org/" + Board.Name + "/res/" + Number + ".json")))
            {
                DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(APIThread));

                APIThread t = dcjs.ReadObject(s) as APIThread;

                List<Post> posts = new List<Post>();
                foreach (APIPost p in t.Posts)
                {
                    UpdatePost(p);
                    posts.Add(Posts[p.Number]);
                }

                return posts;
            }
        }

        public int CompareTo(Thread other)
        {
            if (Posts.Count == 0 || other.Posts.Count == 0) return Posts.Count.CompareTo(other.Posts.Count);
            else return Posts.First().Value.CompareTo(other.Posts.First().Value);
        }

        public void WatchlistChanged()
        {
            //NotifyPropertyChanged("IsWatchlisted");
        }
    }
}
