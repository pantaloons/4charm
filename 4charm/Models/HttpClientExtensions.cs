using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace _4charm.Models
{
    public static class HttpClientExtensions
    {
        private const int BufferLength = 4096; // 4k

        public static async Task<byte[]> GetAsyncWithProgress(this HttpClient client, Uri uri, Action<int> progress)
        {
            List<byte> result = new List<byte>();
            byte[] buffer;
            long bytesRead = 0;

            using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead))
            {
                long totalBytes = long.MaxValue;

                IEnumerable<string> contentLengthValues;
                if (response.Content.Headers.TryGetValues("Content-Length", out contentLengthValues))
                {
                    if (!long.TryParse(contentLengthValues.First(), out totalBytes))
                    {
                        totalBytes = long.MaxValue;
                    }
                }

                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    while (stream.CanRead)
                    {
                        buffer = new byte[BufferLength];
                        var read = await stream.ReadAsync(buffer, 0, BufferLength);

                        if (read > 0)
                        {
                            if (read == BufferLength)
                                result.AddRange(buffer);
                            else
                                result.AddRange(buffer.Take(read));

                            bytesRead += read;

                            if (progress != null)
                            {
                                progress((int)((bytesRead * 100) / totalBytes));
                            }
                        }
                        else
                            break;
                    }

                    if (progress != null)
                    {
                        progress((int)((bytesRead * 100) / totalBytes));
                    }
                }
            }

            return result.ToArray();
        }
    }
}
