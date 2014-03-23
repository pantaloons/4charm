using System.IO;
using System.Threading.Tasks;

namespace _4charm.Controls.Image
{
    interface IPreloadedImage
    {
        Task SetStreamSource(Stream source, string fileType);
        void UnloadStreamSource();
    }
}
