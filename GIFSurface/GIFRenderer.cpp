#include "pch.h"
#include "GIFRenderer.h"
#include <nestegg\nestegg.h>
#include <ppl.h>
#include <ppltasks.h>

using namespace Microsoft::WRL;

GIFRenderer::GIFRenderer() : m_gif(nullptr), m_webm(nullptr), m_packet(nullptr)
{
}

GIFRenderer::~GIFRenderer()
{
	ReleaseResource();
}

void GIFRenderer::Reset()
{
	m_renderedFrame = -1;
	m_frame = 0;
	m_pending = 0;

	m_hasFrame = false;
	m_total = 0;

	if (m_packet != nullptr)
	{
		nestegg_free_packet(m_packet);
		m_packet = nullptr;
	}

	if (m_webm != nullptr)
	{
		nestegg_track_seek(m_webm->m_demux_ctx, 0, 0);
	}
}

void GIFRenderer::SetResource(GIFSurface::GIFImage^ gif)
{
	m_webm = nullptr;
	m_gif = gif;

	m_bufferIdx = 0;
	m_renderedFrame = -1;
	m_frame = 0;
	m_pending = 0;
	m_buffer[0] = std::unique_ptr<uint32_t>(new uint32_t[m_gif->m_gif->SWidth * m_gif->m_gif->SHeight]);
	m_buffer[1] = nullptr;

	m_total = 0;
	m_yuv = nullptr;
	m_hasFrame = false;
	m_packet = nullptr;

	if (m_packet != nullptr)
	{
		nestegg_free_packet(m_packet);
		m_packet = nullptr;
	}
}

void GIFRenderer::SetResource(GIFSurface::WebMImage^ webm)
{
	m_packet = nullptr;
	m_hasFrame = false;
	m_total = 0;

	m_bufferIdx = 0;
	m_renderedFrame = -1;
	m_frame = 0;
	m_pending = 0;
	m_buffer[0] = nullptr;
	m_buffer[1] = nullptr;

	m_yuv = nullptr;
	m_webm = webm;
	m_gif = nullptr;

	if (m_packet != nullptr)
	{
		nestegg_free_packet(m_packet);
		m_packet = nullptr;
	}
}

void GIFRenderer::ReleaseResource()
{
	m_gif = nullptr;
	m_buffer[0] = nullptr;
	m_buffer[1] = nullptr;

	m_yuv = nullptr;
	m_webm = nullptr;

	if (m_packet != nullptr)
	{
		nestegg_free_packet(m_packet);
		m_packet = nullptr;
	}
}

void GIFRenderer::Render(float timeDelta, bool forceUpdate)
{
	if (m_gif)
	{
		RenderGIF(timeDelta, forceUpdate);
	}
	else if (m_webm)
	{
		RenderWebM(timeDelta, forceUpdate);
	}
}

static long long GetTime()
{
	LARGE_INTEGER l2;
	QueryPerformanceCounter(&l2);
	return l2.QuadPart;
}

static long long GetDuration(long long start)
{
	LARGE_INTEGER freq;
	QueryPerformanceFrequency(&freq);

	long long l3 = ((GetTime() - start) * 1000) / freq.QuadPart;
	return l3;
}

void GIFRenderer::RenderWebM(float timeDelta, bool forceUpdate)
{
	uint64_t cacheTimestamp = m_timestamp;

	ScanToNextFrame(timeDelta);

	if (!forceUpdate && cacheTimestamp == m_timestamp)
	{
		return;
	}

	// Sometimes WebM produces no image for a frame. Skip?
	if (!m_image)
	{
		return;
	}

	RenderWebMToSurface(m_image);
}

#define CLIP(x) ((x) < 0 ? 0 : ((x) > 255 ? 255 : (x)))
#define SCALEYUV(v) (((v)+128000)/256000)
static int rcoeff(int y, int u, int v){ return 298082 * y + 0 * u + 408583 * v; }
static int gcoeff(int y, int u, int v){ return 298082 * y - 100291 * u - 208120 * v; }
static int bcoeff(int y, int u, int v){ return 298082 * y + 516411 * u + 0 * v; }

void GIFRenderer::RenderWebMToSurface(vpx_image_t *image)
{
	if (m_yuv == nullptr)
	{
		m_yuv = std::unique_ptr<unsigned char>(new unsigned char[image->d_w * image->d_h * 4]);
		m_yuvw = image->d_w;
		m_yuvh = image->d_h;
	}

	assert(m_yuvw == image->d_w && m_yuvh == image->d_h);

	uint8_t *rgb = m_yuv.get(), *py = image->planes[0], *pu = image->planes[1], *pv = image->planes[2];

	int dw = image->d_w;
	int dh = image->d_h;
	int reduce = 1;

	while (dw > 1024 || dh > 1024)
	{
		dw /= 2;
		dh /= 2;
		reduce *= 2;
	}

	int x = 0;

	for (int j = 0; j < image->d_h; j += reduce)
	{
		for (int i = 0; i < image->d_w; i += reduce)
		{
			int y = py[i] - 16, u = pu[i / 2] - 128, v = pv[i / 2] - 128;
			rgb[0] = CLIP(SCALEYUV(bcoeff(y, u, v)));
			rgb[1] = CLIP(SCALEYUV(gcoeff(y, u, v)));
			rgb[2] = CLIP(SCALEYUV(rcoeff(y, u, v)));
			rgb[3] = 255;
			rgb += 4;
		}

		py += image->stride[0] * reduce;

		if (x & 1)
		{
			pu += image->stride[1] * reduce;
			pv += image->stride[2] * reduce;
		}
		x++;
	}

	CD3D11_TEXTURE2D_DESC textureDescription(DXGI_FORMAT_B8G8R8A8_UNORM, dw, dh, 1, 1);
	D3D11_SUBRESOURCE_DATA data = { m_yuv.get(), dw * sizeof(uint32_t), 0 };

	ComPtr<ID3D11Texture2D> texture;
	ThrowIfFailed(m_d3dDevice->CreateTexture2D(&textureDescription, &data, &texture));

	D3D11_SHADER_RESOURCE_VIEW_DESC shaderViewDescription;
	memset(&shaderViewDescription, 0, sizeof(shaderViewDescription));
	shaderViewDescription.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	shaderViewDescription.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
	shaderViewDescription.Texture2D.MipLevels = 1;

	ComPtr<ID3D11ShaderResourceView> resource;
	ThrowIfFailed(m_d3dDevice->CreateShaderResourceView(texture.Get(), &shaderViewDescription, &resource));

	const float black[] = { 0.0f, 0.0f, 0.0f, 0.0f };
	m_d3dContext->ClearRenderTargetView(m_renderTargetView.Get(), black);
	m_d3dContext->ClearDepthStencilView(m_depthStencilView.Get(), D3D11_CLEAR_DEPTH, 1.0f, 0);
	m_d3dContext->OMSetRenderTargets(1, m_renderTargetView.GetAddressOf(), m_depthStencilView.Get());

	double rw = m_renderTargetSize.Width / (double)dw;
	double rh = m_renderTargetSize.Height / (double)dh;

	double fwidth, fheight, xoff, yoff;
	if (rw <= rh)
	{
		fwidth = m_renderTargetSize.Width;
		fheight = rw * (double)dh;
		xoff = 0;
		yoff = (m_renderTargetSize.Height - fheight) / 2.0;
	}
	else
	{
		fwidth = rh * (double)dw;
		fheight = m_renderTargetSize.Height;
		xoff = (m_renderTargetSize.Width - fwidth) / 2.0;
		yoff = 0;
	}

	RECT rect = { (LONG)xoff, (LONG)yoff, (LONG)(fwidth + xoff), (LONG)(fheight + yoff) };
	m_sprite->Begin();
	m_sprite->Draw(resource.Get(), rect);
	m_sprite->End();
}

void GIFRenderer::ScanToNextFrame(float timeDelta)
{
	timeDelta = timeDelta > 60 ? 60 : timeDelta;
	m_total += timeDelta * 1000 * 1000 * 1000;

	readnextpacket:
	nestegg_packet * pkt = nullptr;
	int r = 1;
	while (m_packet != nullptr || (r = nestegg_read_packet(m_webm->m_demux_ctx, &pkt)) > 0)
	{
		// We cached a packet for the next frame while peeking to see if its timestamp could fit.
		
		if (m_packet != nullptr)
		{
			pkt = m_packet;
		}

		unsigned int track;
		nestegg_packet_track(pkt, &track);

		if (track != m_webm->m_track)
		{
			continue;
		}

		uint64_t timecode;
		nestegg_packet_tstamp(pkt, &timecode);

		m_packet = pkt;

		if (timecode > m_total && m_hasFrame)
		{
			break;
		}

		m_packet = nullptr;
		m_timestamp = timecode;
		m_hasFrame = true;

		unsigned int chunk, chunks;
		nestegg_packet_count(pkt, &chunks);

		// Decode each chunk of data.
		for (chunk = 0; chunk < chunks; ++chunk) {
			unsigned char * data;
			size_t data_size;

			uint64_t tstamp;
			nestegg_packet_data(pkt, chunk, &data, &data_size);

			vpx_codec_err_t err = vpx_codec_decode(&(m_webm->m_decoder), data, data_size, NULL, 1);

			switch (err)
			{
			case VPX_CODEC_ERROR:
			case VPX_CODEC_MEM_ERROR:
			case VPX_CODEC_ABI_MISMATCH:
			case VPX_CODEC_INCAPABLE:
			case VPX_CODEC_UNSUP_BITSTREAM:
			case VPX_CODEC_INVALID_PARAM:
			case VPX_CODEC_LIST_END:
				m_webm = nullptr;
				throw ref new Platform::FailureException(L"Invalid stream.");
				break;
			default:
				break;
			}

			vpx_codec_iter_t iter = NULL;
			m_image = vpx_codec_get_frame(&(m_webm->m_decoder), &iter);
		}

		nestegg_free_packet(pkt);

		if (m_total - timecode > 30 * 1000 * 1000)
		{
			m_total = timecode + 30 * 1000 * 1000;
			break;
		}
	}

	if (r == 0)
	{
		// Skip back to the beginning.
		Reset();
		goto readnextpacket;
	}
}

void GIFRenderer::RenderGIF(float timeDelta, bool forceUpdate)
{
	// Figure out which frame to display next, based on the frame
	// delay timers and the elapsed time delta.
	SelectNextFrame(timeDelta);

	// Typically if the target frame is the same as the previously
	// rendered frame, we don't need to do anything, except sometimes
	// if the buffer got cleared (Disconnected surface, orientation
	// changed, etc), we have to render again anyway. Callers will
	// specify forceupdate in those cases.
	if (!forceUpdate && m_frame == m_renderedFrame) return;
	m_renderedFrame = m_frame;

	// First, blit all the required frames inbetween m_previousFrame
	// and m_frame. Note these will all have disposal mode DISPOSE_DO_NOT
	// or DISPOSE_PREVIOUS since otherwise we would have skipped forward.
	BlitIntermediateFrames();

	// Now copy the last intermediate frame into the swap buffer, and write
	// the final frame overtop of it. This way, if the final frames disposal
	// is DM_PREVIOUS, we still have the previous frame in the other buffer
	// to recover.
	SwapBuffers();

	// Render the target buffer to the DX device.
	RenderToSurface();

	// Update the buffer index and previously rendered frame based on the
	// disposal mode.
	SetupNextFrame();
}

void GIFRenderer::SelectNextFrame(float timeDelta)
{
	m_pending += timeDelta * 1000;
	m_previousFrame = m_frame;

	while (true)
	{
		int nextFrame = (m_frame + 1) % m_gif->m_gif->ImageCount;

		int disposal = m_gif->m_disposals[nextFrame];
		int delay = m_gif->m_delays[nextFrame];

		if (delay > m_pending)
		{
			break;
		}

		switch (disposal)
		{
		case DISPOSAL_UNSPECIFIED:
		case DISPOSE_BACKGROUND:
			m_previousFrame = m_frame;
			break;
		case DISPOSE_DO_NOT:
		case DISPOSE_PREVIOUS:
		default:
			break;
		}

		m_pending -= delay;
		m_frame = nextFrame;

		// Force us to not render more than one frame at once,
		// since this traps the UI thread on low cost devices.
		// This means the GIF is not following the specified
		// speed, but that's probably OK.
		break;
	}

	if (m_frame < m_previousFrame)
	{
		m_previousFrame = 0;
	}
}

void GIFRenderer::BlitIntermediateFrames()
{
	for (int i = m_previousFrame; i < m_frame; i++)
	{
		int disposal = m_gif->m_disposals[i];

		switch (disposal)
		{
		case DISPOSE_DO_NOT:
			BlitFrame(i);
			break;
		case DISPOSE_BACKGROUND:
			ClearBuffer();
			break;
		case DISPOSAL_UNSPECIFIED:
		case DISPOSE_PREVIOUS:
		default:
			// We don't need to render the intermediate frame if it's not
			// going to update the intermediate buffer anyway.
			break;
		}
	}
}

void GIFRenderer::SwapBuffers()
{
	if (!m_buffer[1])
	{
		m_buffer[1] = std::unique_ptr<uint32_t>(new uint32_t[m_gif->m_gif->SWidth * m_gif->m_gif->SHeight]);
	}

	memcpy(m_buffer[(m_bufferIdx + 1) % 2].get(), m_buffer[m_bufferIdx].get(), m_gif->m_gif->SWidth * m_gif->m_gif->SHeight * sizeof uint32_t);

	// Blit next frame
	m_bufferIdx = (m_bufferIdx + 1) % 2;
	BlitFrame(m_frame);
}

void GIFRenderer::RenderToSurface()
{
	CD3D11_TEXTURE2D_DESC textureDescription(DXGI_FORMAT_B8G8R8A8_UNORM, m_gif->m_gif->SWidth, m_gif->m_gif->SHeight, 1, 1);
	D3D11_SUBRESOURCE_DATA data = { m_buffer[m_bufferIdx].get(), m_gif->m_gif->SWidth * sizeof (uint32_t), 0 };

	ComPtr<ID3D11Texture2D> texture;
	ThrowIfFailed(m_d3dDevice->CreateTexture2D(&textureDescription, &data, &texture));

	D3D11_SHADER_RESOURCE_VIEW_DESC shaderViewDescription;
	memset(&shaderViewDescription, 0, sizeof (shaderViewDescription));
	shaderViewDescription.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
	shaderViewDescription.ViewDimension = D3D11_SRV_DIMENSION_TEXTURE2D;
	shaderViewDescription.Texture2D.MipLevels = 1;

	ComPtr<ID3D11ShaderResourceView> resource;
	ThrowIfFailed(m_d3dDevice->CreateShaderResourceView(texture.Get(), &shaderViewDescription, &resource));

	const float black[] = { 0.0f, 0.0f, 0.0f, 0.0f };
	m_d3dContext->ClearRenderTargetView(m_renderTargetView.Get(), black);
	m_d3dContext->ClearDepthStencilView(m_depthStencilView.Get(), D3D11_CLEAR_DEPTH, 1.0f, 0);
	m_d3dContext->OMSetRenderTargets(1, m_renderTargetView.GetAddressOf(), m_depthStencilView.Get());

	double rw = m_renderTargetSize.Width / (double)m_gif->m_gif->SWidth;
	double rh = m_renderTargetSize.Height / (double)m_gif->m_gif->SHeight;

	double fwidth, fheight, xoff, yoff;
	if (rw <= rh)
	{
		fwidth = m_renderTargetSize.Width;
		fheight = rw * (double)m_gif->m_gif->SHeight;
		xoff = 0;
		yoff = (m_renderTargetSize.Height - fheight) / 2.0;
	}
	else
	{
		fwidth = rh * (double)m_gif->m_gif->SWidth;
		fheight = m_renderTargetSize.Height;
		xoff = (m_renderTargetSize.Width - fwidth) / 2.0;
		yoff = 0;
	}

	RECT rect = { (LONG)xoff, (LONG)yoff, (LONG)(fwidth + xoff), (LONG)(fheight + yoff) };
	m_sprite->Begin();
	m_sprite->Draw(resource.Get(), rect);
	m_sprite->End();
}

void GIFRenderer::SetupNextFrame()
{
	int disposal = m_gif->m_disposals[m_frame];

	switch (disposal)
	{
	case DISPOSAL_UNSPECIFIED:
	case DISPOSE_DO_NOT:
		m_previousFrame = (m_frame + 1) % m_gif->m_gif->ImageCount;
		break;
	case DISPOSE_BACKGROUND:
		m_previousFrame = m_frame;
		break;
	case DISPOSE_PREVIOUS:
		m_previousFrame = (m_frame + 1) % m_gif->m_gif->ImageCount;
		m_bufferIdx = (m_bufferIdx + 1) % 2;
		break;
	default:
		// Corrupt disposal.
		m_previousFrame = m_frame;
	}
}

void GIFRenderer::ClearBuffer()
{
	GifColorType color = { 255, 255, 255 };
	if (m_gif->m_gif->SColorMap != nullptr
		&& m_gif->m_gif->SBackGroundColor >= 0
		&& m_gif->m_gif->SBackGroundColor < m_gif->m_gif->SColorMap->ColorCount)
	{
		color = m_gif->m_gif->SColorMap->Colors[m_gif->m_gif->SBackGroundColor];
	}

	unsigned int bgColor = ((uint32_t)color.Blue << 0) | ((uint32_t)color.Green << 8) | ((uint32_t)color.Red << 16) | ((uint32_t)255 << 24);

	for (int y = 0; y < m_gif->m_gif->SHeight; y++)
	{
		for (int x = 0; x < m_gif->m_gif->SWidth; x++)
		{
			m_buffer[m_bufferIdx].get()[y * m_gif->m_gif->SWidth + x] = bgColor;
		}
	}
}

void GIFRenderer::BlitFrame(int frame)
{
	SavedImage image = m_gif->m_gif->SavedImages[frame];
	GifByteType *bits = image.RasterBits;

	ColorMapObject *colorMap = nullptr;
	if (image.ImageDesc.ColorMap != nullptr)
	{
		colorMap = image.ImageDesc.ColorMap;
	}
	else if (m_gif->m_gif->SColorMap != nullptr)
	{
		colorMap = m_gif->m_gif->SColorMap;
	}

	int transparent = m_gif->m_transparencies[frame];

	// Sometimes GIFs have invalid inner bounds specified.
	int minx = max(0, min(image.ImageDesc.Left, m_gif->m_gif->SWidth));
	int miny = max(0, min(image.ImageDesc.Top, m_gif->m_gif->SHeight));
	int maxy = max(0, min(image.ImageDesc.Top + image.ImageDesc.Height, m_gif->m_gif->SHeight));
	int maxx = max(0, min(image.ImageDesc.Left + image.ImageDesc.Width, m_gif->m_gif->SWidth));

	int i = 0;
	for (int y = miny; y < maxy; y++)
	{
		for (int x = minx; x < maxx; x++)
		{
			int offset = y * m_gif->m_gif->SWidth + x;
			GifByteType colorIndex = bits[i++];

			if (transparent != colorIndex && colorIndex >= 0 && colorIndex < colorMap->ColorCount)
			{
				GifColorType color = colorMap->Colors[colorIndex];
				m_buffer[m_bufferIdx].get()[offset] = ((uint32_t)color.Blue << 0) | ((uint32_t)color.Green << 8) | ((uint32_t)color.Red << 16) | ((uint32_t)255 << 24);
			}
		}
	}
}