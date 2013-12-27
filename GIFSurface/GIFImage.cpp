#include "pch.h"
#include "GIFImage.h"

static const int FRAME_MINDELAY = 60;
static const int FRAME_DEFAULTDELAY = 100;

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
	static bool GetGraphicsControlBlock(SavedImage image, GraphicsControlBlock *gcb)
	{
		for (int i = image.ExtensionBlockCount - 1; i >= 0; i--)
		{
			ExtensionBlock eb = image.ExtensionBlocks[i];
			if (eb.Function == GRAPHICS_EXT_FUNC_CODE)
			{
				if (DGifExtensionToGCB(eb.ByteCount, eb.Bytes, gcb) == GIF_ERROR)
				{
					throw ref new Platform::Exception(E_FAIL, "Couldn't decode frame GCB.");
				}

				return true;
			}
		}

		return false;
	}

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

		m_delays = std::vector<int>(gif->ImageCount, FRAME_DEFAULTDELAY);
		m_disposals = std::vector<int>(gif->ImageCount, DISPOSAL_UNSPECIFIED);
		m_transparencies = std::vector<int>(gif->ImageCount, -1);

		for (int i = 0; i < gif->ImageCount; i++)
		{
			GraphicsControlBlock gcb;
			if (GetGraphicsControlBlock(gif->SavedImages[i], &gcb))
			{
				m_disposals[i] = gcb.DisposalMode;
				m_transparencies[i] = gcb.TransparentColor;

				if (10 * gcb.DelayTime < FRAME_MINDELAY)
				{
					m_delays[i] = FRAME_DEFAULTDELAY;
				}
				else
				{
					m_delays[i] = 10 * gcb.DelayTime;
				}
			}
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

				 throw ref new Platform::FailureException(ref new Platform::String(GifErrorString(error)));
			}

			m_gif = nullptr;
		}
	}
}