#include "pch.h"
#include "GIFWrapper.h"
#include "Direct3DContentProvider.h"

#include <ppltasks.h>

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
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		ComPtr<Direct3DContentProvider> provider = Make<Direct3DContentProvider>(this);
		return reinterpret_cast<IDrawingSurfaceContentProvider^>(provider.Get());
	}

	void GIFWrapper::SetGIFSource(GIFImage^ gif)
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		// Forcibly render the first frame of the GIF.
		m_isDirty = true;
		m_timer->Reset();
		if (m_renderer)
		{
			m_renderer->SetGIFResource(gif);
			m_renderer->Render(0, true);

			RequestAdditionalFrame();
		}

		m_gif = gif;
	}

	void GIFWrapper::UnloadGIF()
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		if (m_renderer)
		{
			if (m_renderer)
			{
				m_renderer->ReleaseGIFResource();
			}
			m_isActive = false;
		}

		m_gif = nullptr;
	}

	void GIFWrapper::ShouldAnimate::set(bool shouldAnimate)
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		m_isActive = shouldAnimate;
		m_isDirty = true;

		if (!m_isActive)
		{
			// Forcibly render one frame if the GIF is now inactive.
			m_timer->Reset();
			
			if (m_renderer)
			{
				m_renderer->Reset();
				m_renderer->Render(0, true);
			}
		}

		RequestAdditionalFrame();
	}

	void GIFWrapper::RenderResolution::set(Windows::Foundation::Size renderResolution)
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		if (renderResolution.Width  != m_renderResolution.Width ||
			renderResolution.Height != m_renderResolution.Height)
		{
			m_renderResolution = renderResolution;

			if (m_renderer)
			{
				m_renderer->UpdateForRenderResolutionChange(m_renderResolution.Width, m_renderResolution.Height);
				RecreateSynchronizedTexture();

				if (!m_isActive)
				{
					// We need to force render a frame if the GIF was inactive, since the previous
					// frame got released when we recreated the texture.
					m_isDirty = true;
					m_renderer->Render(0, true);
				}
			}
		}
	}

	// Interface With Direct3DContentProvider
	HRESULT GIFWrapper::Connect(_In_ IDrawingSurfaceRuntimeHostNative* host)
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		m_renderer = ref new GIFRenderer();
		m_renderer->Initialize();
		m_renderer->UpdateForWindowSizeChange(WindowBounds.Width, WindowBounds.Height);
		m_renderer->UpdateForRenderResolutionChange(m_renderResolution.Width, m_renderResolution.Height);

		// Restart timer after renderer has finished initializing.
		m_timer->Reset();
		if (m_gif != nullptr)
		{
			m_isDirty = true;
			m_renderer->SetGIFResource(m_gif);
			m_renderer->Render(0, true);

			RequestAdditionalFrame();
		}

		return S_OK;
	}

	void GIFWrapper::Disconnect()
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		m_renderer->ReleaseGIFResource();
		m_renderer = nullptr;
	}

	HRESULT GIFWrapper::PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Out_ BOOL* contentDirty)
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		*contentDirty = m_isActive || m_isDirty;

		return S_OK;
	}

	HRESULT GIFWrapper::GetTexture(_In_ const DrawingSurfaceSizeF* size, _Inout_ IDrawingSurfaceSynchronizedTextureNative** synchronizedTexture, _Inout_ DrawingSurfaceRectF* textureSubRectangle)
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		if (!m_isActive)
		{
			// Even if the image is inactive, we should blit a
			// frame if the surface is dirty.
			if (m_isDirty)
			{
				m_renderer->Render(0, true);
				m_isDirty = false;
			}
			
			return S_OK;
		}

		m_timer->Update();
		m_renderer->Render(m_timer->Delta, m_isDirty);
		m_isDirty = false;

		RequestAdditionalFrame();

		return S_OK;
	}

	ID3D11Texture2D* GIFWrapper::GetTexture()
	{
		std::lock_guard<std::recursive_mutex> lock(m_mutex);

		return m_renderer->GetTexture();
	}
}