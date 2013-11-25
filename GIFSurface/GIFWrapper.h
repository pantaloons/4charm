#pragma once

#include <wrl/module.h>
#include <DrawingSurfaceNative.h>
#include "GIFRenderer.h"
#include "BasicTimer.h"

namespace GIFSurface
{
	public delegate void RequestAdditionalFrameHandler();
	public delegate void RecreateSynchronizedTextureHandler();

	[Windows::Foundation::Metadata::WebHostHidden]
    public ref class GIFWrapper sealed
    {
    public:
		GIFWrapper();

		Windows::Phone::Graphics::Interop::IDrawingSurfaceContentProvider^ CreateContentProvider();
		void SetGIF(const Platform::Array<unsigned char>^ resource);
		void UnloadGIF();

		event RequestAdditionalFrameHandler^ RequestAdditionalFrame;
		event RecreateSynchronizedTextureHandler^ RecreateSynchronizedTexture;

		property Windows::Foundation::Size WindowBounds;
		property Windows::Foundation::Size NativeResolution;
		property Windows::Foundation::Size RenderResolution
		{
			Windows::Foundation::Size get(){ return m_renderResolution; }
			void set(Windows::Foundation::Size renderResolution);
		}

	internal:
		HRESULT STDMETHODCALLTYPE Connect(_In_ IDrawingSurfaceRuntimeHostNative* host);
		void STDMETHODCALLTYPE Disconnect();
		HRESULT STDMETHODCALLTYPE PrepareResources(_In_ const LARGE_INTEGER* presentTargetTime, _Out_ BOOL* contentDirty);
		HRESULT STDMETHODCALLTYPE GetTexture(_In_ const DrawingSurfaceSizeF* size, _Inout_ IDrawingSurfaceSynchronizedTextureNative** synchronizedTexture, _Inout_ DrawingSurfaceRectF* textureSubRectangle);
		ID3D11Texture2D* GetTexture();

	private:
		GIFRenderer^ m_renderer;
		BasicTimer^ m_timer;
		Windows::Foundation::Size m_renderResolution;
		const Platform::Array<unsigned char>^ m_resource;
    };
}