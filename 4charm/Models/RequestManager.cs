using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

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

        private HttpClient _client;
        private RequestManager()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("4charm", Version));
        }

        public async Task<HttpResponseMessage> GetAsync(Uri uri)
        {
            return await _client.GetAsync(EnforceHTTPS(uri));
        }

        public async Task<Stream> GetStreamAsync(Uri uri)
        {
            return await _client.GetStreamAsync(EnforceHTTPS(uri));
        }

        public async Task<string> GetStringAsync(Uri uri)
        {
            return await _client.GetStringAsync(EnforceHTTPS(uri));
        }

        public async Task<byte[]> GetByteArrayWithProgressAsync(Uri uri, Action<int> progress, CancellationToken token)
        {
            return await _client.GetAsyncWithProgress(EnforceHTTPS(uri), progress, token);
        }

        public async Task<HttpResponseMessage> PostAsync(Uri uri, Dictionary<string, string> fields)
        {
            return await _client.PostAsync(EnforceHTTPS(uri), new FormUrlEncodedContent(fields));
        }


        public async Task<HttpResponseMessage> PostAsync(Uri uri, Dictionary<string, string> fields, string fileName, byte[] imageData)
        {
            ByteArrayContent file = new ByteArrayContent(imageData);

            MultipartFormDataContent form = new MultipartFormDataContent();
            foreach (KeyValuePair<string, string> field in fields) form.Add(new StringContent(field.Value), "\"" + field.Key + "\"");
            form.Add(file, "\"upfile\"", "\"" + fileName + "\"");

            return await _client.PostAsync(EnforceHTTPS(uri), form);
        }

        public Uri EnforceHTTPS(Uri uri)
        {
            Uri modURI = uri;
            if (CriticalSettingsManager.Current.EnableHTTPS && modURI.Scheme != "https")
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

        private static string _version;
        private static string Version
        {
            get
            {
                if (_version == null)
                {
                    string appManifestName = "WMAppManifest.xml";
                    string appNodeName = "App";

                    var settings = new XmlReaderSettings();
                    settings.XmlResolver = new XmlXapResolver();

                    using (XmlReader rdr = XmlReader.Create(appManifestName, settings))
                    {
                        rdr.ReadToDescendant(appNodeName);
                        if (!rdr.IsStartElement())
                        {
                            throw new System.FormatException(appManifestName + " is missing " + appNodeName);
                        }

                        _version = rdr.GetAttribute("Version");
                    }
                }

                return _version;
            }
        }
    }
}
