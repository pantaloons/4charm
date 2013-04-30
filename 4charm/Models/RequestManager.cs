using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace _4charm.Models
{
    class RequestManager
    {
        private static RequestManager _current;
        public static RequestManager Current
        {
            get
            {
                if (_current == null) _current = new RequestManager();
                return _current;
            }
        }

        private HttpClient client;
        private RequestManager()
        {
            client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("4charm", App.Version));
        }

        public async Task<Stream> GetStreamAsync(Uri uri)
        {
            return await client.GetStreamAsync(EnforceHTTPS(uri));
        }

        public async Task<string> GetStringAsync(Uri uri)
        {
            return await client.GetStringAsync(EnforceHTTPS(uri));
        }

        public async Task<HttpResponseMessage> PostAsync(Uri uri, Uri referrer, Dictionary<string, string> fields)
        {
            client.DefaultRequestHeaders.Referrer = EnforceHTTPS(referrer);
            return await client.PostAsync(EnforceHTTPS(uri), new FormUrlEncodedContent(fields));
            client.DefaultRequestHeaders.Referrer = null;
        }

        public Uri EnforceHTTPS(Uri uri)
        {
            Uri modURI = uri;
            if (SettingsManager.Current.EnableHTTPS && modURI.Scheme != "https")
            {
                string x = modURI.AbsoluteUri;
                if (!x.StartsWith("http://"))
                {
                    throw new InvalidOperationException("Cannot make non-http request with enforced HTTPS.");
                }
                modURI = new Uri("https" + modURI.AbsoluteUri.Substring(4));
            }
            return modURI;
        }
    }
}
