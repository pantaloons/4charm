#include "pch.h"
#include "GIFRenderer.h"

using namespace Microsoft::WRL;

GIFRenderer::GIFRenderer()
{
}

void GIFRenderer::CreateDeviceResources()
{
	Direct3DBase::CreateDeviceResources();
}

void GIFRenderer::CreateWindowSizeDependentResources()
{
	Direct3DBase::CreateWindowSizeDependentResources();
}

void GIFRenderer::Update(float timeTotal, float timeDelta)
{
	(void) timeDelta; // Unused parameter.
	(void) timeTotal; // Unused parameter.
}

void GIFRenderer::Render()
{
	const float midnightBlue[] = { 0.098f, 0.098f, 0.439f, 1.000f };
	m_d3dContext->ClearRenderTargetView(
		m_renderTargetView.Get(),
		midnightBlue
		);
}