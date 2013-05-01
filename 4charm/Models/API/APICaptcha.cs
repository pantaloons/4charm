using System.Runtime.Serialization;

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
