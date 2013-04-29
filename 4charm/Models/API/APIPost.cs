using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace _4charm.Models.API
{
    [DataContract]
    public sealed class APIPost
    {
        [DataMember(Name = "no", IsRequired = true)]
        public ulong Number { get; set; }

        [DataMember(Name = "resto", IsRequired = true)]
        public ulong ReplyTo { get; set; }

        [DataMember(Name = "sticky")]
        public bool IsSticky { get; set; }

        [DataMember(Name = "closed")]
        public bool IsClosed { get; set; }

        public DateTime Time { get; set; }
        [DataMember(Name = "now", IsRequired = true)]
        public string TimeSet
        {
            set
            {
                DateTime t;
                if (DateTime.TryParseExact(value, "MM/dd/yy(ddd)HH:mm", new CultureInfo("en-US"), DateTimeStyles.None, out t))
                {
                    Time = t;
                }
                else if (DateTime.TryParseExact(value, "MM/dd/yy(ddd)HH:mm:ss", new CultureInfo("en-US"), DateTimeStyles.None, out t))
                {
                    Time = t;
                }
            }
            get
            {
                return null;
            }
        }

        [DataMember(Name = "time", IsRequired = true)]
        public ulong TimeStamp { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "trip")]
        public string Tripcode { get; set; }

        [DataMember(Name = "id")]
        public string ID { get; set; }

        public enum CapCodes
        {
            none,
            mod,
            quiet_mod,
            admin,
            admin_highlight,
            developer
        };
        public CapCodes CapCode { get; set; }
        [DataMember(Name = "capcode")]
        public string CapCodeSet
        {
            set
            {
                CapCodes c;
                if (Enum.TryParse<CapCodes>(value, out c))
                {
                    CapCode = c;
                }
            }
            get
            {
                return null;
            }
        }

        [DataMember(Name = "country")]
        public string CountryCode { get; set; }

        [DataMember(Name = "country_name")]
        public string CountryName { get; set; }

        [DataMember(Name = "email")]
        public string Email { get; set; }

        [DataMember(Name = "sub")]
        public string Subject { get; set; }

        [DataMember(Name = "com")]
        public string Comment { get; set; }

        [DataMember(Name = "tim")]
        public ulong RenamedFileName { get; set; }

        [DataMember(Name = "filename")]
        public string FileName { get; set; }

        public enum FileTypes
        {
            jpg,
            png,
            gif,
            pdf,
            swf
        };
        public FileTypes FileType { get; set; }
        [DataMember(Name = "ext")]
        public string FileTypeSet
        {
            set
            {
                if (value == null) return;

                FileTypes f;
                if (Enum.TryParse<FileTypes>(value.Substring(1), out f))
                {
                    FileType = f;
                }
            }
            get
            {
                return null;
            }
        }

        [DataMember(Name = "fsize")]
        public uint FileSize { get; set; }

        [DataMember(Name = "md5")]
        public string md5 { get; set; }

        [DataMember(Name = "w")]
        public uint ImageWidth { get; set; }

        [DataMember(Name = "h")]
        public uint ImageHeight { get; set; }

        [DataMember(Name = "tn_w")]
        public uint ThumbnailWidth { get; set; }

        [DataMember(Name = "tn_h")]
        public uint ThumbnailHeight { get; set; }

        [DataMember(Name = "filedeleted")]
        public bool FileDeleted { get; set; }

        [DataMember(Name = "spoiler")]
        public bool Spoiler { get; set; }

        [DataMember(Name = "custom_spoiler")]
        public uint CustomSpoiner { get; set; }

        [DataMember(Name = "omitted_posts")]
        public uint OmittedPosts { get; set; }

        [DataMember(Name = "omitted_images")]
        public uint OmittedImages { get; set; }

        [DataMember(Name = "replies")]
        public uint Replies { get; set; }

        [DataMember(Name = "images")]
        public uint Images { get; set; }

        [DataMember(Name = "bumplimit")]
        public bool BumpLimit { get; set; }

        [DataMember(Name = "imagelimit")]
        public bool ImageLimit { get; set; }
    }
}
