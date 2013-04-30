using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace _4charm.Models.API
{
    [DataContract]
    public class APICaptcha
    {
        [DataMember(Name = "challenge", IsRequired = true)]
        public string Challenge { get; set; }

        [DataMember(Name = "timeout", IsRequired = true)]
        public ulong Timeout { get; set; }
    }
}
