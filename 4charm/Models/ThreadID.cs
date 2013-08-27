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
        [DataMember]
        public string BoardName { get; set; }

        [DataMember]
        public ulong Number { get; set; }

        [DataMember]
        public Post Initial { get; set; }

        public ThreadID(string boardName, ulong number, Post initial)
        {
            BoardName = boardName;
            Number = number;
            Initial = initial;
        }
    }
}
