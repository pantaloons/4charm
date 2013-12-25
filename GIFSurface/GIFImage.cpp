#include "pch.h"
#include "GIFImage.h"

struct GifArrayData
{
	int Position;
	int Length;
	unsigned char *Data;
};

static int ArraySlurp(GifFileType *gft, GifByteType *buffer, int length)
{
	GifArrayData *input = (GifArrayData *)gft->UserData;

	if (input->Position == input->Length)
	{
		return 0;
	}
	length = min(length, input->Length - input->Position);
	memcpy(buffer, input->Data + input->Position, length);
	input->Position += length;

	return length;
}

namespace GIFSurface
{
	GIFImage::GIFImage(const Platform::Array<unsigned char>^ resource)
	{
		int error = 0;
		GifArrayData data = { 0, resource->Length, resource->Data };
		GifFileType *gif = DGifOpen(&data, ArraySlurp, &error);
		if (error)
		{
			throw ref new Platform::FailureException(ref new Platform::String(GifErrorString(error)));
		}

		if (DGifSlurp(gif) == GIF_ERROR)
		{
			int error = gif->Error;
			if (DGifCloseFile(gif) == GIF_ERROR)
			{
				free(gif);
			}

			throw ref new Platform::FailureException(ref new Platform::String(GifErrorString(error)));
		}

		m_gif = gif;
	}

	GIFImage::~GIFImage()
	{
		if (m_gif)
		{
			if (DGifCloseFile(m_gif) == GIF_ERROR)
			{
				// We didn't modify the GIF, so this technically should never happen.
				// Just refree after getting the error, and ignore for now.
				int error = m_gif->Error;
				free(m_gif);
				m_gif = nullptr;

				// throw ref new Platform::FailureException(ref new Platform::String(GifErrorString(error)));
			}

			m_gif = nullptr;
		}
	}
}