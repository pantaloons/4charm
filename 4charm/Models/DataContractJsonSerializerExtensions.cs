using System.IO;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace _4charm.Models
{
    /// <summary>
    /// Extension to easily allow parsing JSON responses on a background thread.
    /// 
    /// For very large threads, (>1000) posts, the parsing would actually cause
    /// blanking when scrolling in the thread view without this, and have large
    /// UI stalls of several seconds.
    /// </summary>
    static class DataContractJsonSerializerExtensions
    {
        /// <summary>
        /// Read a stream into a datacontract asyncronously, off the UI thread.
        /// </summary>
        /// <typeparam name="T">The type of object to read out.</typeparam>
        /// <param name="serializer">The serializer to read the object on.</param>
        /// <param name="s">The stream to read from.</param>
        /// <returns>The parsed object.</returns>
        public static async Task<T> ReadObjectAsync<T>(this DataContractJsonSerializer serializer, Stream s) where T : class
        {
            return await Task.Run<T>(() =>
            {
                return serializer.ReadObject(s) as T;
            });
        }
    }
}
