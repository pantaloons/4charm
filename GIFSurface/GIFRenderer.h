#pragma once

#include "Direct3DBase.h"
#include "GIFImage.h"
#include <GIFLIB\gif_lib.h>
#include <SpriteBatch\SpriteBatch.h>

#include <memory>
#include <mutex>

ref class GIFRenderer sealed : public Direct3DBase
{
public:
	GIFRenderer();
	virtual ~GIFRenderer();

	// Direct3DBase methods.
	virtual void Render(float timeDelta, bool forceUpdate) override;

	// Method for loading GIF
internal:
	void Reset();
	void SetGIFResource(GIFSurface::GIFImage^ gif);
	void ReleaseGIFResource();

private:
	void SelectNextFrame(float timeDelta);
	void BlitIntermediateFrames();
	void SwapBuffers();
	void RenderToSurface();
	void SetupNextFrame();

	void BlitFrame(int frame);
	void ClearBuffer();
	
	GIFSurface::GIFImage^ m_gif;
	int m_frame;
	int m_renderedFrame;
	int m_previousFrame;
	float m_pending;

	Microsoft::WRL::ComPtr<ID3D11Texture2D> m_texture;
	Microsoft::WRL::ComPtr<ID3D11ShaderResourceView> m_resource;

	int m_bufferIdx;
	std::unique_ptr<uint32_t> m_buffer[2];
};
