#include "pch.h"
#include "WebMImage.h"

#include <libvpx\vpx\vp8dx.h>

static int
nestegg_read_cb(void *buffer, size_t length, void *userdata) {
	WebMArrayData *input = (WebMArrayData *)userdata;

	if (input->Position == input->Length)
	{
		return 0;
	}

	length = min(length, input->Length - input->Position);
	memcpy(buffer, input->Data + input->Position, length);
	input->Position += length;

	if (input->Position == input->Length)
	{
		return 0;
	}
	else
	{
		return 1;
	}
}


static int
nestegg_seek_cb(int64_t offset, int whence, void *userdata) {
	WebMArrayData *input = (WebMArrayData *)userdata;

	switch (whence) {
	case NESTEGG_SEEK_SET:
		if (offset < 0 || offset > input->Length) return -1;
		input->Position = offset;
		break;
	case NESTEGG_SEEK_CUR:
		if (offset + input->Position > input->Length) return -1;
		input->Position += offset;
		break;
	case NESTEGG_SEEK_END:
		if (input->Length + offset > input->Length) return -1;
		input->Position += offset;
		break;
	};

	return 0;
}


static int64_t
nestegg_tell_cb(void *userdata) {
	WebMArrayData *input = (WebMArrayData *)userdata;

	return input->Position;
}

namespace GIFSurface
{
	WebMImage::WebMImage(const Platform::Array<unsigned char>^ resource)
	{
		nestegg_io io = { nestegg_read_cb, nestegg_seek_cb, nestegg_tell_cb, 0 };

		m_resource = std::unique_ptr<unsigned char>(new unsigned char[resource->Length]);
		memcpy(m_resource.get(), resource->Data, resource->Length);
		m_data.Position = 0;
		m_data.Length = resource->Length;
		m_data.Data = m_resource.get();
		io.userdata = &m_data;

		if (nestegg_init(&m_demux_ctx, io, NULL, -1))
		{
			throw ref new Platform::FailureException(L"Failed to initialize nestegg.");
		}

		unsigned int tracks;
		if (nestegg_track_count(m_demux_ctx, &tracks))
		{
			nestegg_destroy(m_demux_ctx);
			throw ref new Platform::FailureException(L"Failed to get number of tracks.");
		}

		int track = -1;
		for (int i = 0; i < tracks; i++)
		{
			if (nestegg_track_type(m_demux_ctx, i) == NESTEGG_TRACK_VIDEO)
			{
				track = i;
				break;
			}
		}

		if (track == -1)
		{
			nestegg_destroy(m_demux_ctx);
			throw ref new Platform::FailureException(L"No video track found.");
		}

		int codec = nestegg_track_codec_id(m_demux_ctx, track);
		if (codec != NESTEGG_CODEC_VP8 && codec != NESTEGG_CODEC_VP9)
		{
			nestegg_destroy(m_demux_ctx);
			throw ref new Platform::FailureException(L"Invalid track codec.");
		}

		vpx_codec_ctx_t decoder;
		if (vpx_codec_dec_init(&decoder, codec == NESTEGG_CODEC_VP8 ? vpx_codec_vp8_dx() : vpx_codec_vp9_dx(), NULL, 0))
		{
			//Platform::String^ error = ref new Platform::String(vpx_codec_error(&decoder));
			vpx_codec_destroy(&decoder);
			throw ref new Platform::FailureException(L"");
			/*throw ref new Platform::FailureException(error);*/
		}

		m_track = track;
		m_decoder = decoder;
	}

	WebMImage::~WebMImage()
	{
		m_resource = nullptr;
		nestegg_destroy(m_demux_ctx);
		vpx_codec_destroy(&m_decoder);
	}
}