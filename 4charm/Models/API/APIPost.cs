using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace _4charm.Models.API
{
    /// <summary>
    /// 4chan API post type.
    /// </summary>
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

        /// <summary>
        /// "now" doesn't parse into a DateTime by default, so we use the TimeSet field to transfer
        /// that API field into a DateTime at parse time.
        /// </summary>
        [DataMember(Name = "now", IsRequired = true)]
        public string Time { get; set; }

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

        /// <summary>
        /// CapCodes are returned by the API as a string, which doesn't transfer into
        /// the CapCodes enum, so we use this CapCodeSet field to do the parse time
        /// translation.
        /// </summary>
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
            swf,
            webm
        };
        public FileTypes FileType { get; set; }

        /// <summary>
        /// Extensions are returned as strings with a prefix "." by the API, which don't
        /// map to the FileTypes enum. Instead we use the FileTypeSet field to transfer
        /// the property at parse time.
        /// </summary>
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
