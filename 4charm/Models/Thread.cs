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
    /// <summary>
    /// Representation of a thread on the site. Note that threads are unique, there will never be multiple instances
    /// of "/fa/ - 1111111", etc.
    /// 
    /// Threads should never actually be created, instead you should fetch a desired thread out of the cache using
    /// ThreadCache.EnforceBoard("fa").EnforceBoard(1111111). This ensures uniqueness and consistency of thread instances
    /// across the application.
    /// 
    /// A thread contains a list of posts, a link to the parent board, and some minor metadata about the threads. Threads are not
    /// serialized, cached, or saved in any way between application runs.
    /// </summary>
    public class Thread
    {
        /// <summary>
        /// Parent board object out of the cache.
        /// </summary>
        public Board Board { get; set; }

        /// <summary>
        /// Thread ID, the number of the initial post. 
        /// 
        /// Note these can be shared between boards, so the real thread key is (boardName, Number).
        /// </summary>
        public ulong Number { get; set; }

        /// <summary>
        /// Convenience property linking to the first post of the thread.
        /// </summary>
        public Post Initial { get { return Posts.FirstOrDefault().Value; } }

        /// <summary>
        /// If the thread is a sticky.
        /// </summary>
        public bool IsSticky { get { return Initial != null ? Initial.IsSticky : false; } }

        /// <summary>
        /// The subject of the thread.
        /// </summary>
        public string Subject { get { return Initial != null ? Initial.Subject : null; } }

        /// <summary>
        /// The number of image replies (excludes the initial post) of the thread. This is used to determine
        /// if the image viewer cache is stale when navigating to full screen flip view.
        /// </summary>
        public ulong ImageCount { get { return Initial != null ? Initial.ImageCount : 0; } }

        /// <summary>
        /// List of posts in the thread. This is a sorted list, and accurately represents the order of posts in the thread.
        /// Posts do not get removed while the application is running, so information continuously accumulates in this dictionary,
        /// as replies get posted, or updated.
        /// 
        /// This gets thrown out when the application is terminated.
        /// 
        /// If new information is available to overwrite existing post information, it should be "Merged"
        /// in with the appropriate Merge API.
        /// </summary>
        public SortedDictionary<ulong, Post> Posts { get; set; }

        /// <summary>
        /// Construct a thread from an initial post and a board.
        /// </summary>
        /// <param name="board">The parent board out of the board cache.</param>
        /// <param name="op">The first post in the thread.</param>
        public Thread(Board board, APIPost op)
        {
            Board = board;
            Number = op.Number;
            Posts = new SortedDictionary<ulong, Post>();
            Merge(op);
        }

        /// <summary>
        /// We can technically construct a thread with only a number and no initial post.
        /// 
        /// This shouldn't be displayed in the UI, but is useful for some internal serialization manipulations.
        /// </summary>
        /// <param name="board">The parent board out of the board cache.</param>
        /// <param name="number">The thread ID.</param>
        public Thread(Board board, ulong number)
        {
            Board = board;
            Number = number;
            Posts = new SortedDictionary<ulong, Post>();
        }

        /// <summary>
        /// Merge a post into the thread. If the post is already in the thread, it will be updated
        /// in-place with the new information, otherwise the post is inserted in the correct position.
        /// 
        /// This is only used by the serialization consturcts to create fake thread views for watchlist
        /// and history, since all other merged info comes from the API and is thus an APIPost object.
        /// </summary>
        /// <param name="op">The post to merge into the thread.</param>
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
       
        /// <summary>
        /// Merge a post into the thread. If the post is already in the thread, it will be updated
        /// in-place with the new information, otherwise the post is inserted in the correct position.
        /// </summary>
        /// <param name="p">The post to merge into the thread.</param>
        public void Merge(APIPost p)
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

        /// <summary>
        /// Reload information for the thread. This gets all the post information from the API, updates the cache in-place,
        /// and returns a new list of all the posts in the thread.
        /// </summary>
        /// <returns>A list of the posts in the thread.</returns>
        public async Task<List<Post>> GetPostsAsync()
        {
            using (Stream s = await RequestManager.Current.GetStreamAsync(new Uri("http://api.4chan.org/" + Board.Name + "/res/" + Number + ".json")))
            {
                DataContractJsonSerializer dcjs = new DataContractJsonSerializer(typeof(APIThread));

                APIThread t = await dcjs.ReadObjectAsync<APIThread>(s);

                List<Post> posts = new List<Post>();
                foreach (APIPost p in t.Posts)
                {
                    Merge(p);
                    posts.Add(Posts[p.Number]);
                }

                return posts;
            }
        }
    }
}
