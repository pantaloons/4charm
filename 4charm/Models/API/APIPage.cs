using System.Collections.Generic;
using System.Runtime.Serialization;

namespace _4charm.Models.API
{
    [DataContract]
    public sealed class APIPage
    {
        [DataMember(Name = "page", IsRequired = true)]
        public ulong Page { get; set; }

        [DataMember(Name = "threads", IsRequired = true)]
        public List<APIPost> Threads { get; set; }
    }
}
