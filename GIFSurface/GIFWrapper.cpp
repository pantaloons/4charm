#include "pch.h"
#include "GIFWrapper.h"
#include "Direct3DContentProvider.h"

using namespace Windows::Foundation;
using namespace Windows::UI::Core;
using namespace Microsoft::WRL;
using namespace Windows::Phone::Graphics::Interop;
using namespace Windows::Phone::Input::Interop;

namespace GIFSurface
{
	GIFWrapper::GIFWrapper() :
		m_timer(ref new BasicTimer())
	{
	}

	IDrawingSurfaceContentProvider^ GIFWrapper::CreateContentProvider()
	{
		ComPtr<Direct3DContentProvider> provider = Make<Direct3DContentProvider>(this);
		return reinterpret_cast<IDrawingSurfaceContentProvider^>(provider.Get());
	}

	void GIFWrapper::SetGIF(const Platform::Array<unsigned char>^ resource)
	{
		m_resource = resource;
		if (m_renderer)
		{
			m_renderer->CreateGIFResources(m_resource);
		}
	}

	void GIFWrapper::UnloadGIF()
	{
		if (m_renderer)
		{
			m_renderer->ReleaseGIFResources();
		}
	}

	void GIFWrapper::RenderResolution::set(Windows::Foundation::Size renderResolution)
	{
		if (renderResolution.Width  != m_renderResolution.Width ||
			renderResolution.Height != m_renderResolution.Height)
		{
			m_renderResolution = renderResolution;

			if (m_renderer)
			{
				m_renderer->UpdateForRenderResolutionChange(m_renderResolution.Width, m_renderResolution.Height);
				RecreateSynchronizedTexture();
			}
		}
	}

	// Interface With Direct3DContentProvider
	HRESULT GIFWrapper::Connect(_In_ IDrawingSurfaceRuntimeHostNative* host)
	{
		m_renderer = ref new GIFRenderer();
		m_renderer->Initialize();
		m_renderer->UpdateForWindowSizeChange(WindowBounds.Width, WindowBounds.Height);
		m_renderer->UpdateForRenderResolutionChange(m_renderResolution.Width, m_renderResolution.Height);

		// Restart timer after renderer has finished initializing.
		m_timer->Reset();

		return S_OK;
	}

	void GIFWrapper::Disconnect()
	{
		m_renderer->ReleaseGIFResources();
		m_renderer = nullptr;
	}

	HRESULT GIFWrapper::PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Out_ BOOL* contentDirty)
	{
		*contentDirty = true;

		return S_OK;
	}

	HRESULT GIFWrapper::GetTexture(_In_ const DrawingSurfaceSizeF* size, _Inout_ IDrawingSurfaceSynchronizedTextureNative** synchronizedTexture, _Inout_ DrawingSurfaceRectF* textureSubRectangle)
	{
		m_timer->Update();
		m_renderer->Render(m_timer->Delta);

		RequestAdditionalFrame();

		return S_OK;
	}

	ID3D11Texture2D* GIFWrapper::GetTexture()
	{
		return m_renderer->GetTexture();
	}
}