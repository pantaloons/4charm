using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Phone.Controls
{
    public interface IDisplayableImage
    {
        Uri ImageSrc { get; }
        uint ImageWidth { get; }
        uint ImageHeight { get; }
    }
}
