using System.Runtime.Serialization;

namespace _4charm.Models
{
    [DataContract]
    public class BoardID
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public bool IsNSFW { get; set; }

        public BoardID(string name, string description, bool isNSFW)
        {
            Name = name;
            Description = description;
            IsNSFW = isNSFW;
        }
    }
}
