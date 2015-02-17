#pragma once

#include <wrl/module.h>
#include <DrawingSurfaceNative.h>
#include "GIFImage.h"
#include "GIFRenderer.h"
#include "WebMImage.h"
#include "BasicTimer.h"

namespace GIFSurface
{
	public delegate void RequestAdditionalFrameHandler();
	public delegate void RecreateSynchronizedTextureHandler();

	[Windows::Foundation::Metadata::WebHostHidden]
    public ref class AnimatedWrapper sealed
    {
    public:
		AnimatedWrapper();

		Windows::Phone::Graphics::Interop::IDrawingSurfaceContentProvider^ CreateContentProvider();
		
		void SetSource(GIFImage^ gif);
		void SetSource(WebMImage^ webm);
		void Unload();

		event RequestAdditionalFrameHandler^ RequestAdditionalFrame;
		event RecreateSynchronizedTextureHandler^ RecreateSynchronizedTexture;

		property Windows::Foundation::Size WindowBounds;
		property Windows::Foundation::Size NativeResolution;
		property Windows::Foundation::Size RenderResolution
		{
			Windows::Foundation::Size get(){ return m_renderResolution; }
			void set(Windows::Foundation::Size renderResolution);
		}
		property bool ShouldAnimate
		{
			bool get(){ return m_isActive; }
			void set(bool value);
		}

	internal:
		HRESULT STDMETHODCALLTYPE Connect(_In_ IDrawingSurfaceRuntimeHostNative* host);
		void STDMETHODCALLTYPE Disconnect();
		HRESULT STDMETHODCALLTYPE PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Out_ BOOL* contentDirty);
		HRESULT STDMETHODCALLTYPE GetTexture(_In_ const DrawingSurfaceSizeF* size, _Inout_ IDrawingSurfaceSynchronizedTextureNative** synchronizedTexture, _Inout_ DrawingSurfaceRectF* textureSubRectangle);
		ID3D11Texture2D* GetTexture();

	private:
		bool m_isActive;
		bool m_isDirty;

		GIFImage^ m_gif;
		WebMImage^ m_webm;
		GIFRenderer^ m_renderer;
		std::recursive_mutex m_mutex;

		BasicTimer^ m_timer;
		Windows::Foundation::Size m_renderResolution;
    };
}