using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace _4charm.Controls
{
    interface IPreloadedImage
    {
        Task<bool> SetStreamSource(Stream source, string fileType, CancellationToken token);
        void UnloadStreamSource();
    }
}
