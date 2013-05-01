using System;
using System.Text.RegularExpressions;
using System.Windows.Navigation;

namespace _4charm.Models
{
    public class UriMapper : UriMapperBase
    {
        private static Regex r = new Regex("/Threads\\.xaml\\?board=([a-zA-Z0-9]+)");
        public override Uri MapUri(Uri uri)
        {
            string link = uri.ToString();

            Match m = r.Match(link);
            if (m.Success) return new Uri("/Views/ThreadsPage.xaml?board=" + m.Groups[1].Value);
            else return uri;
        }
    }
}
