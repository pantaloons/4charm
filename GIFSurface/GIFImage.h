#pragma once

#include <GIFLIB\gif_lib.h>
#include <memory>

using namespace Windows::Storage::Streams;

namespace GIFSurface
{
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class GIFImage sealed
	{
	public:
		GIFImage(const Platform::Array<unsigned char>^ resource);
		virtual ~GIFImage();

	internal:
		GifFileType *m_gif;
	};
}