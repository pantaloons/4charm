using System.Collections.Generic;
using System.Runtime.Serialization;

namespace _4charm.Models.API
{
    /// <summary>
    /// 4chan API page type. Pages are basically just lists of threads.
    /// </summary>
    [DataContract]
    public sealed class APIPage
    {
        /// <summary>
        /// List of threads on the page, in correct bump order.
        /// </summary>
        [DataMember(Name = "threads", IsRequired = true)]
        public List<APIPost> Threads { get; set; }
    }
}
