using System.Runtime.Serialization;

namespace _4charm.Models
{
    /// <summary>
    /// The bare minimum amount of information needed to show a serialized thread in the watchlist or
    /// history views. This is the post number, post board, and information about the first post in the
    /// thread.
    /// </summary>
    [DataContract]
    public class ThreadID
    {
        /// <summary>
        /// Name of the board, like "fa".
        /// </summary>
        [DataMember]
        public string BoardName { get; set; }

        /// <summary>
        /// Initial post number in the thread, or the thread ID.
        /// </summary>
        [DataMember]
        public ulong Number { get; set; }

        /// <summary>
        /// Initial post object. Only the first post is needed since subsequent posts are not shown in the
        /// history and watchlist views.
        /// </summary>
        [DataMember]
        public Post Initial { get; set; }

        /// <summary>
        /// Construct a new ThreadID ready to be serialized. Should only be used by save/restore code.
        /// </summary>
        /// <param name="boardName">The name of the board this thread is on.</param>
        /// <param name="number">The ID of the thread.</param>
        /// <param name="initial">The first post of the thread.</param>
        public ThreadID(string boardName, ulong number, Post initial)
        {
            BoardName = boardName;
            Number = number;
            Initial = initial;
        }
    }
}
