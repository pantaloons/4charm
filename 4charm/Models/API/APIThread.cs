using System.Collections.Generic;
using System.Runtime.Serialization;

namespace _4charm.Models.API
{
    /// <summary>
    /// 4chan API thread type. Just a thread ID (number), and child list of posts.
    /// 
    /// The board name is implicit in whatever caller fetched this, and is not returned by the API.
    /// </summary>
    [DataContract]
    public sealed class APIThread
    {
        /// <summary>
        /// List of posts in the thread, in correct thread post order.
        /// </summary>
        [DataMember(Name = "posts", IsRequired = true)]
        public List<APIPost> Posts { get; set; }
    }
}
