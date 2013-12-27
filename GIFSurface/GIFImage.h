#pragma once

#include <GIFLIB\gif_lib.h>
#include <vector>

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
		std::vector<int> m_delays;
		std::vector<int> m_disposals;
		std::vector<int> m_transparencies;
		GifFileType *m_gif;
	};
}