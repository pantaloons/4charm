using System.IO;

namespace _4charm.Controls
{
    interface IPreloadedImage
    {
        Stream StreamSource { get; set; }

        int PixelWidth { get; }
        int PixelHeight { get; }
    }
}
