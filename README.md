# 4charm
4chan client for Windows Phone 8

### Contributing
Feel free to submit a pull request. New board tile images are appreciated.

### Structure

#### 4charm
Windows Phone 8.0 project.

#### GIFSurface
Native code renderer of GIF and WebM images. Implements a DirectX swapchain that is composited by XAML on top of app content. GIFs and WebMs are decoded by their respective libraries, this library implements WinRT interop, animation timing, and performs blitting of the surfaces. Dynamically linked by 4charm.dll

#### GIFLib
Third party library for decoding GIFs to bitmaps. Statically linked into GIFSurface.dll

#### libvpx
Third party library for decoding VP8/VP9 streams, with a couple of changes to link correctly on WinRT. This is not a project, but code that is shared by the libvpx_arm and libvpx_x86 projects.

#### libvpx_arm / libvpx_x86
Wrapper Visual Studio project around libvpx. These projects are automatically generated from the libvpx sources. Statically linked into GIFSurface.dll

#### nestegg
Third party library to read WebM files. Chunks are read from the file by nestegg, and then passed to libvpx to be sequentially decoded. Statically linked by GIFSurface.dll

#### SpriteBatch
Helper library for rendering bitmaps decoded by GIFLib/libvpx to the screen. Statically linked by GIFSurface.dll

### Building

The project won't build out of the box on versions of Visual Studio newer than VS2012. The `vpx_arm` project won't build
without a copy of `msvcdis110.dll` located at `%PROGRAMFILES%\Microsoft Visual Studio 11.0\Common7\IDE\msvcdis110.dll`.
The easiest way to get this DLL is to install VS2012 Express.

Additionally, the `packages.config` file includes a package that supplies the `System.Xml.XPath` library from the Silverlight 4
SDK. `System.Xml.XPath` is required for HTMLAgilityPack for WP, but isn't included in .NET for Windows Phone and you won't have the
DLL if you don't have the Silverlight 4 SDK already installed. Hopefully, the inclusion of this package will bridge the gap between
systems with SL4 and systems without. 