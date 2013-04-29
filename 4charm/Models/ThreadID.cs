using System.Runtime.Serialization;

namespace _4charm.Models
{
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
