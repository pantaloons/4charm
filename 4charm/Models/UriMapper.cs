using System;
using System.Text.RegularExpressions;
using System.Windows.Navigation;

namespace _4charm.Models
{
    /// <summary>
    /// Translate old pinned board tiles to new URI scheme.
    /// </summary>
    public class UriMapper : UriMapperBase
    {
        /// <summary>
        /// Matches the old pinned board tile URI scheme. Board name is in group 1.
        /// </summary>
        private static Regex r = new Regex("/Threads\\.xaml\\?board=([a-zA-Z0-9]+)");

        public override Uri MapUri(Uri uri)
        {
            string link = uri.ToString();

            Match m = r.Match(link);
            if (m.Success) return new Uri("/Views/ThreadsPage.xaml?board=" + m.Groups[1].Value, UriKind.Relative);
            else return uri;
        }
    }
}
