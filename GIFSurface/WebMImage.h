#pragma once

#include <libvpx\vpx\vpx_decoder.h>
#include <vector>
#include <nestegg\nestegg.h>

#include <memory>

using namespace Windows::Storage::Streams;

struct WebMArrayData
{
	int Position;
	int Length;
	unsigned char *Data;
};

namespace GIFSurface
{
	[Windows::Foundation::Metadata::WebHostHidden]
	public ref class WebMImage sealed
	{
	public:
		WebMImage(const Platform::Array<unsigned char>^ resource);
		virtual ~WebMImage();

	internal:
		std::unique_ptr<unsigned char> m_resource;
		WebMArrayData m_data;
		nestegg * m_demux_ctx;
		vpx_codec_ctx_t m_decoder;
		int m_track;
	};
}