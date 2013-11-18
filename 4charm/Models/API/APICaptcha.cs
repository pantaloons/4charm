using System.Runtime.Serialization;

namespace _4charm.Models.API
{
    /// <summary>
    /// Captcha response type returned by ReCaptcha.
    /// </summary>
    [DataContract]
    public class APICaptcha
    {
        /// <summary>
        /// Captcha challenge key, must be submitted along with response.
        /// </summary>
        [DataMember(Name = "challenge", IsRequired = true)]
        public string Challenge { get; set; }

        /// <summary>
        /// Duration before captcha times out, and a new one should be used.
        /// </summary>
        [DataMember(Name = "timeout", IsRequired = true)]
        public ulong Timeout { get; set; }
    }
}
