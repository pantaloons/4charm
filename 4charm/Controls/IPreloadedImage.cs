using System.IO;

namespace _4charm.Controls
{
    interface IPreloadedImage
    {
        void SetStreamSource(Stream source, string fileType);
        void UnloadStreamSource();

        int PixelWidth { get; }
        int PixelHeight { get; }
    }
}
