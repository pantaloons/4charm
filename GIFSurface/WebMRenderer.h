#pragma once

#include "Direct3DBase.h"
#include "WebMImage.h"
#include "WebMRenderer.h"

#include <memory>
#include <mutex>

ref class WebMRenderer sealed : public Direct3DBase
{
public:
	WebMRenderer();
	virtual ~WebMRenderer();

	// Direct3DBase methods.
	virtual void Render(float timeDelta, bool forceUpdate) override;

	// Method for loading WebM
internal:
	void Reset();
	void SetResource(GIFSurface::WebMImage^ webm);
	void ReleaseResource();
internal:


private:

};
