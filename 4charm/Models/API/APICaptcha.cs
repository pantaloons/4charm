using System.Runtime.Serialization;

namespace _4charm.Models.API
{
    /// <summary>
    /// Captcha response type returned by ReCaptcha.
    /// </summary>
    [DataContract]
    public class APICaptcha
    {
        [DataMember(Name = "challenge", IsRequired = true)]
        public string Challenge { get; set; }

        [DataMember(Name = "timeout", IsRequired = true)]
        public ulong Timeout { get; set; }
    }
}
