#pragma once

#include "Direct3DBase.h"
#include "GIFImage.h"
#include "WebMImage.h"
#include <GIFLIB\gif_lib.h>
#include <SpriteBatch\SpriteBatch.h>
#include <libvpx\vpx\vpx_decoder.h>

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
	void SetResource(GIFSurface::GIFImage^ gif);
	void SetResource(GIFSurface::WebMImage^ webm);
	void ReleaseResource();

private:
	// GIF stuff
	void RenderGIF(float timeDelta, bool forceUpdate);
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

	int m_bufferIdx;
	std::unique_ptr<uint32_t> m_buffer[2];

	// WebM stuff
	void RenderWebM(float timeDelta, bool forceUpdate);
	void ScanToNextFrame(float timeDelta);
	void RenderWebMToSurface(vpx_image_t *image);

	bool m_hasFrame;
	nestegg_packet *m_packet;
	uint64_t m_timestamp;
	float m_total;
	unsigned int m_yuvw, m_yuvh;

	std::unique_ptr<unsigned char> m_yuv;

	vpx_image_t *m_image;
	GIFSurface::WebMImage^ m_webm;
};
