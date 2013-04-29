using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

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
