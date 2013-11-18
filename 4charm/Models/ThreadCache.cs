using System.Collections.Generic;

namespace _4charm.Models
{
    /// <summary>
    /// Global cache of threads. Cache structure is:
    /// 
    /// List<Board> -> List<Thread> -> List<Post>
    /// 
    /// The cache has no notification mechanism and listeners will not dynamically update. But,
    /// the cache can be updated at any time-- and both posts and threads can updated in-place
    /// with the associated "Merge" function. Subsequent requests from the cache will return
    /// the new merged information.
    /// 
    /// The cache is not saved and is purged on app termination.
    /// </summary>
    class ThreadCache
    {
        /// <summary>
        /// Singleton for the cache service.
        /// </summary>
        private static ThreadCache _current;
        public static ThreadCache Current
        {
            get
            {
                if (_current == null) _current = new ThreadCache();
                return _current;
            }
        }

        /// <summary>
        /// List of boards currently existing in the cache. A board cannot be navigated to
        /// by the application without existing in here.
        /// </summary>
        public Dictionary<string, Board> Boards { get; set; }

        /// <summary>
        /// Private singleton constructor.
        /// </summary>
        private ThreadCache()
        {
            Boards = new Dictionary<string, Board>();
        }

        /// <summary>
        /// Ensures that a board exists in the cache, by creating it from the global board list
        /// if it doesn't.
        /// </summary>
        /// <param name="name">The name of the board</param>
        /// <returns>The instance of that board in the cache. May be newly created.</returns>
        public Board EnforceBoard(string name)
        {
            if (!Boards.ContainsKey(name))
            {
                Boards[name] = new Board(BoardList.Boards[name].Name, BoardList.Boards[name].Description, BoardList.Boards[name].IsNSFW);
            }
            return Boards[name];
        }
    }
}
