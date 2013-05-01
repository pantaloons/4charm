using System.Collections.Generic;
using System.Runtime.Serialization;

namespace _4charm.Models.API
{
    [DataContract]
    public sealed class APIThread
    {
        [DataMember(Name = "posts", IsRequired = true)]
        public List<APIPost> Posts { get; set; }

        public ulong Number { get { return Posts.Count == 0 ? 0 : Posts[0].Number; } }
    }
}
