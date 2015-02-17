# 4charm
4chan client for Windows Phone

### Contributing
Feel free to submit a pull request. New board tile images are appreciated.

### Structure

#### 4charm
Windows Phone 8.0 project. Does everything except render GIFs and WebMs.

#### GIFSurface
Native code renderer of GIF and WebM images. Implements a DirectX swapchain that is composited by XAML on top of app content. GIFs and WebMs are decoded by their respective libraries, this library implements WinRT interop, animation timing, and performs blitting of the surfaces. Dynamically linked by 4charm.dll

#### GIFLib
Third party library for decoding GIFs to bitmaps. Statically linked into GIFSurface.dll

#### libvpx
Third party library for decoding VP8/VP9 streams, with a couple of changes to link correctly on WinRT. This is not a project, but code that is shared by the libvpx_arm and libvpx_x86 projects.

#### libvpx_arm / libvpx_x86
Wrapper Visual Studio project around libvpx. These projects are automatically generated from the libvpx sources. Statically linked into GIFSurface.dll

#### nestegg
Third party library to read WebM files. Chunks are read from the file by nestegg, and then passed to libvpx to be sequentially decoded.

#### SpriteBatch
Helper library for rendering bitmaps decoded by GIFLib/libvpx to the screen.
