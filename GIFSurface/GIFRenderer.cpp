#include "pch.h"
#include "GIFRenderer.h"

using namespace Microsoft::WRL;

static const int FRAME_MINDELAY = 60;

struct GifArrayData
{
	int Position;
	int Length;
	unsigned char *Data;
};

GIFRenderer::GIFRenderer()
{
	m_gif = nullptr;
}

GIFRenderer::~GIFRenderer()
{
	ReleaseGIFResources();
}

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

void GIFRenderer::CreateGIFResources(const Platform::Array<unsigned char>^ resource)
{
	std::lock_guard<std::mutex> lock(m_mutex);

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

	m_bufferIdx = 0;
	m_renderedFrame = -1;
	m_frame = 0;
	m_pending = 0;
	m_buffer[0] = std::unique_ptr<uint32_t>(new uint32_t[m_gif->SWidth * m_gif->SHeight]);
	m_buffer[1] = nullptr;
}

void GIFRenderer::ReleaseGIFResources()
{
	std::lock_guard<std::mutex> lock(m_mutex);

	m_buffer[0] = nullptr;
	m_buffer[1] = nullptr;

	if (m_gif)
	{
		if (DGifCloseFile(m_gif) == GIF_ERROR)
		{
			int error = m_gif->Error;
			free(m_gif);
			m_gif = nullptr;
			throw ref new Platform::FailureException(ref new Platform::String(GifErrorString(error)));
		}

		m_gif = nullptr;
	}
}

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

void GIFRenderer::Render(float timeDelta)
{
	std::lock_guard<std::mutex> lock(m_mutex);

	if (!m_gif) return;

	SelectNextFrame(timeDelta);

	if (m_frame == m_renderedFrame) return;
	m_renderedFrame = m_frame;

	BlitIntermediateFrames();
	SwapBuffers();
	RenderToSurface();
	SetupNextFrame();
}

void GIFRenderer::SelectNextFrame(float timeDelta)
{
	m_pending += timeDelta * 1000;
	m_previousFrame = m_frame;

	while (true)
	{
		int nextFrame = (m_frame + 1) % m_gif->ImageCount;

		int disposal = DISPOSAL_UNSPECIFIED;
		int delay = FRAME_MINDELAY;

		GraphicsControlBlock gcb;
		if (GetGraphicsControlBlock(m_gif->SavedImages[nextFrame], &gcb))
		{
			disposal = gcb.DisposalMode;
			delay = max(FRAME_MINDELAY, 10 * gcb.DelayTime);
		}

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
		default:
			break;
		}

		m_pending -= delay;
		m_frame = nextFrame;
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
		int disposal = DISPOSAL_UNSPECIFIED;

		GraphicsControlBlock gcb;
		if (GetGraphicsControlBlock(m_gif->SavedImages[i], &gcb))
		{
			disposal = gcb.DisposalMode;
		}

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
		m_buffer[1] = std::unique_ptr<uint32_t>(new uint32_t[m_gif->SWidth * m_gif->SHeight]);
	}

	memcpy(m_buffer[(m_bufferIdx + 1) % 2].get(), m_buffer[m_bufferIdx].get(), m_gif->SWidth * m_gif->SHeight * sizeof uint32_t);

	// Blit next frame
	m_bufferIdx = (m_bufferIdx + 1) % 2;
	BlitFrame(m_frame);
}

void GIFRenderer::RenderToSurface()
{
	CD3D11_TEXTURE2D_DESC textureDescription(DXGI_FORMAT_B8G8R8A8_UNORM, m_gif->SWidth, m_gif->SHeight, 1, 1);
	D3D11_SUBRESOURCE_DATA data = { m_buffer[m_bufferIdx].get(), m_gif->SWidth * sizeof (uint32_t), 0 };

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

	double rw = m_renderTargetSize.Width / (double)m_gif->SWidth;
	double rh = m_renderTargetSize.Height / (double)m_gif->SHeight;

	double fwidth, fheight, xoff, yoff;
	if (rw <= rh)
	{
		fwidth = m_renderTargetSize.Width;
		fheight = rw * (double)m_gif->SHeight;
		xoff = 0;
		yoff = (m_renderTargetSize.Height - fheight) / 2.0;
	}
	else
	{
		fwidth = rh * (double)m_gif->SWidth;
		fheight = m_renderTargetSize.Height;
		xoff = (m_renderTargetSize.Width - fwidth) / 2.0;
		yoff = 0;
	}

	RECT rect = { xoff, yoff, fwidth + xoff, fheight + yoff };
	m_sprite->Begin();
	m_sprite->Draw(resource.Get(), rect);
	m_sprite->End();
}

void GIFRenderer::SetupNextFrame()
{
	int disposal = DISPOSAL_UNSPECIFIED;

	GraphicsControlBlock gcb;
	if (GetGraphicsControlBlock(m_gif->SavedImages[m_frame], &gcb))
	{
		disposal = gcb.DisposalMode;
	}

	switch (disposal)
	{
	case DISPOSAL_UNSPECIFIED:
	case DISPOSE_DO_NOT:
		m_previousFrame = (m_frame + 1) % m_gif->ImageCount;
		break;
	case DISPOSE_BACKGROUND:
		m_previousFrame = m_frame;
		break;
	case DISPOSE_PREVIOUS:
		m_previousFrame = (m_frame + 1) % m_gif->ImageCount;
		m_bufferIdx = (m_bufferIdx + 1) % 2;
		break;
	}
}

void GIFRenderer::ClearBuffer()
{
	GifColorType color = { 255, 255, 255 };
	if (m_gif->SColorMap != nullptr) color = m_gif->SColorMap->Colors[m_gif->SBackGroundColor];

	unsigned int bgColor = ((uint32_t)color.Blue << 0) | ((uint32_t)color.Green << 8) | ((uint32_t)color.Red << 16) | ((uint32_t)255 << 24);

	for (int y = 0; y < m_gif->SHeight; y++)
	{
		for (int x = 0; x < m_gif->SWidth; x++)
		{
			m_buffer[m_bufferIdx].get()[y * m_gif->SHeight + x] = bgColor;
		}
	}
}

void GIFRenderer::BlitFrame(int frame)
{
	SavedImage image = m_gif->SavedImages[frame];
	GifByteType *bits = image.RasterBits;

	ColorMapObject *colorMap = nullptr;
	if (image.ImageDesc.ColorMap != nullptr)
	{
		colorMap = image.ImageDesc.ColorMap;
	}
	else if (m_gif->SColorMap != nullptr)
	{
		colorMap = m_gif->SColorMap;
	}

	int i = 0;
	for (int y = image.ImageDesc.Top; y < image.ImageDesc.Top + image.ImageDesc.Height; y++)
	{
		for (int x = image.ImageDesc.Left; x < image.ImageDesc.Left + image.ImageDesc.Width; x++)
		{
			int offset = y * m_gif->SWidth + x;
			GifByteType colorIndex = bits[i++];

			GraphicsControlBlock gcb;
			if (!GetGraphicsControlBlock(m_gif->SavedImages[frame], &gcb) || gcb.TransparentColor != colorIndex)
			{
				GifColorType color = colorMap->Colors[colorIndex];
				m_buffer[m_bufferIdx].get()[offset] = ((uint32_t)color.Blue << 0) | ((uint32_t)color.Green << 8) | ((uint32_t)color.Red << 16) | ((uint32_t)255 << 24);
			}
		}
	}
}