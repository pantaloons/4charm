using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
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

        private HttpClient _client;
        private RequestManager()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("4charm", App.Version));
        }

        public async Task<Stream> GetStreamAsync(Uri uri)
        {
            return await _client.GetStreamAsync(EnforceHTTPS(uri));
        }

        public async Task<string> GetStringAsync(Uri uri)
        {
            return await _client.GetStringAsync(EnforceHTTPS(uri));
        }

        public async Task<HttpResponseMessage> PostAsync(Uri uri, Uri referrer, Dictionary<string, string> fields)
        {
            _client.DefaultRequestHeaders.Referrer = referrer;
            HttpResponseMessage response;
            try
            {
                response = await _client.PostAsync(EnforceHTTPS(uri), new FormUrlEncodedContent(fields));
            }
            catch
            {
                _client.DefaultRequestHeaders.Referrer = null;
                throw;
            }
            _client.DefaultRequestHeaders.Referrer = null;
            return response;
        }


        public async Task<HttpResponseMessage> PostAsync(Uri uri, Uri referrer, Dictionary<string, string> fields, string fileName, byte[] imageData)
        {
            ByteArrayContent file = new ByteArrayContent(imageData);

            MultipartFormDataContent form = new MultipartFormDataContent();
            foreach (KeyValuePair<string, string> field in fields) form.Add(new StringContent(field.Value), "\"" + field.Key + "\"");
            form.Add(file, "\"upfile\"", "\"" + fileName + "\"");

            _client.DefaultRequestHeaders.Referrer = referrer;
            HttpResponseMessage response;
            try
            {
                response = await _client.PostAsync(EnforceHTTPS(uri), form);
            }
            catch
            {
                _client.DefaultRequestHeaders.Referrer = null;
                throw;
            }
            _client.DefaultRequestHeaders.Referrer = null;
            return response;
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
    }
}
