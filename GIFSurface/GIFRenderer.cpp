#include "pch.h"
#include "GIFRenderer.h"

using namespace Microsoft::WRL;

GIFRenderer::GIFRenderer() : m_gif(nullptr)
{
}

GIFRenderer::~GIFRenderer()
{
	ReleaseGIFResource();
}

void GIFRenderer::Reset()
{
	m_renderedFrame = -1;
	m_frame = 0;
	m_pending = 0;
}

void GIFRenderer::SetGIFResource(GIFSurface::GIFImage^ gif)
{
	m_gif = gif;

	m_bufferIdx = 0;
	m_renderedFrame = -1;
	m_frame = 0;
	m_pending = 0;
	m_buffer[0] = std::unique_ptr<uint32_t>(new uint32_t[m_gif->m_gif->SWidth * m_gif->m_gif->SHeight]);
	m_buffer[1] = nullptr;
}

void GIFRenderer::ReleaseGIFResource()
{
	m_gif = nullptr;
	m_buffer[0] = nullptr;
	m_buffer[1] = nullptr;
}

void GIFRenderer::Render(float timeDelta, bool forceUpdate)
{
	if (!m_gif) return;

	// Figure out what the next frame is on, based on the frame
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
	}
}

void GIFRenderer::ClearBuffer()
{
	GifColorType color = { 255, 255, 255 };
	if (m_gif->m_gif->SColorMap != nullptr) color = m_gif->m_gif->SColorMap->Colors[m_gif->m_gif->SBackGroundColor];

	unsigned int bgColor = ((uint32_t)color.Blue << 0) | ((uint32_t)color.Green << 8) | ((uint32_t)color.Red << 16) | ((uint32_t)255 << 24);

	for (int y = 0; y < m_gif->m_gif->SHeight; y++)
	{
		for (int x = 0; x < m_gif->m_gif->SWidth; x++)
		{
			m_buffer[m_bufferIdx].get()[y * m_gif->m_gif->SHeight + x] = bgColor;
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

	int i = 0;
	for (int y = image.ImageDesc.Top; y < image.ImageDesc.Top + image.ImageDesc.Height; y++)
	{
		for (int x = image.ImageDesc.Left; x < image.ImageDesc.Left + image.ImageDesc.Width; x++)
		{
			int offset = y * m_gif->m_gif->SWidth + x;
			GifByteType colorIndex = bits[i++];

			if (transparent != colorIndex)
			{
				GifColorType color = colorMap->Colors[colorIndex];
				m_buffer[m_bufferIdx].get()[offset] = ((uint32_t)color.Blue << 0) | ((uint32_t)color.Green << 8) | ((uint32_t)color.Red << 16) | ((uint32_t)255 << 24);
			}
		}
	}
}