using System;
using System.Text.RegularExpressions;
using System.Windows.Navigation;

namespace _4charm.Models
{
    /// <summary>
    /// Translate old pinned board tiles to new URI scheme. We cannot actually migrate tile URIs,
    /// so this can't go into the VersionMigrator.
    /// </summary>
    public class UriMapper : UriMapperBase
    {
        /// <summary>
        /// Matches the old pinned board tile URI scheme. Board name is in group 1.
        /// </summary>
        private static Regex r = new Regex("/Threads\\.xaml\\?board=([a-zA-Z0-9]+)");

        /// <summary>
        /// Uri mapper override to do the actual translation.
        /// </summary>
        /// <param name="uri">The URI to map.</param>
        /// <returns>The mapped URI.</returns>
        public override Uri MapUri(Uri uri)
        {
            string link = uri.ToString();

            Match m = r.Match(link);
            if (m.Success) return new Uri("/Views/ThreadsPage.xaml?board=" + m.Groups[1].Value, UriKind.Relative);
            else return uri;
        }
    }
}
