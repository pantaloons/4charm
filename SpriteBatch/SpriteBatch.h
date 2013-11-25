//--------------------------------------------------------------------------------------
// File: SpriteBatch.h
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.
//
// http://go.microsoft.com/fwlink/?LinkId=248929
//--------------------------------------------------------------------------------------

#pragma once

#include <d3d11_1.h>
#include <DirectXMath.h>
#include <DirectXColors.h>
#include <functional>
#include <memory>


namespace DirectX
{
    #if (DIRECTXMATH_VERSION < 305) && !defined(XM_CALLCONV)
    #define XM_CALLCONV __fastcall
    typedef const XMVECTOR& HXMVECTOR;
    typedef const XMMATRIX& FXMMATRIX;
    #endif

    enum SpriteSortMode
    {
        SpriteSortMode_Deferred,
        SpriteSortMode_Immediate,
        SpriteSortMode_Texture,
        SpriteSortMode_BackToFront,
        SpriteSortMode_FrontToBack,
    };
    
    
    enum SpriteEffects
    {
        SpriteEffects_None = 0,
        SpriteEffects_FlipHorizontally = 1,
        SpriteEffects_FlipVertically = 2,
        SpriteEffects_FlipBoth = SpriteEffects_FlipHorizontally | SpriteEffects_FlipVertically,
    };

    
    class SpriteBatch
    {
    public:
        explicit SpriteBatch(_In_ ID3D11DeviceContext* deviceContext);
        SpriteBatch(SpriteBatch&& moveFrom);
        SpriteBatch& operator= (SpriteBatch&& moveFrom);
        virtual ~SpriteBatch();

        // Begin/End a batch of sprite drawing operations.
        void XM_CALLCONV Begin(SpriteSortMode sortMode = SpriteSortMode_Deferred, _In_opt_ ID3D11BlendState* blendState = nullptr, _In_opt_ ID3D11SamplerState* samplerState = nullptr, _In_opt_ ID3D11DepthStencilState* depthStencilState = nullptr, _In_opt_ ID3D11RasterizerState* rasterizerState = nullptr, _In_opt_ std::function<void()> setCustomShaders = nullptr, FXMMATRIX transformMatrix = MatrixIdentity);
        void End();

        // Draw overloads specifying position, origin and scale as XMFLOAT2.
        void XM_CALLCONV Draw(_In_ ID3D11ShaderResourceView* texture, XMFLOAT2 const& position, FXMVECTOR color = Colors::White);
        void XM_CALLCONV Draw(_In_ ID3D11ShaderResourceView* texture, XMFLOAT2 const& position, _In_opt_ RECT const* sourceRectangle, FXMVECTOR color = Colors::White, float rotation = 0, XMFLOAT2 const& origin = Float2Zero, float scale = 1, SpriteEffects effects = SpriteEffects_None, float layerDepth = 0);
        void XM_CALLCONV Draw(_In_ ID3D11ShaderResourceView* texture, XMFLOAT2 const& position, _In_opt_ RECT const* sourceRectangle, FXMVECTOR color, float rotation, XMFLOAT2 const& origin, XMFLOAT2 const& scale, SpriteEffects effects = SpriteEffects_None, float layerDepth = 0);

        // Draw overloads specifying position, origin and scale via the first two components of an XMVECTOR.
        void XM_CALLCONV Draw(_In_ ID3D11ShaderResourceView* texture, FXMVECTOR position, FXMVECTOR color = Colors::White);
        void XM_CALLCONV Draw(_In_ ID3D11ShaderResourceView* texture, FXMVECTOR position, _In_opt_ RECT const* sourceRectangle, FXMVECTOR color = Colors::White, float rotation = 0, FXMVECTOR origin = g_XMZero, float scale = 1, SpriteEffects effects = SpriteEffects_None, float layerDepth = 0);
        void XM_CALLCONV Draw(_In_ ID3D11ShaderResourceView* texture, FXMVECTOR position, _In_opt_ RECT const* sourceRectangle, FXMVECTOR color, float rotation, FXMVECTOR origin, GXMVECTOR scale, SpriteEffects effects = SpriteEffects_None, float layerDepth = 0);

        // Draw overloads specifying position as a RECT.
        void XM_CALLCONV Draw(_In_ ID3D11ShaderResourceView* texture, RECT const& destinationRectangle, FXMVECTOR color = Colors::White);
        void XM_CALLCONV Draw(_In_ ID3D11ShaderResourceView* texture, RECT const& destinationRectangle, _In_opt_ RECT const* sourceRectangle, FXMVECTOR color = Colors::White, float rotation = 0, XMFLOAT2 const& origin = Float2Zero, SpriteEffects effects = SpriteEffects_None, float layerDepth = 0);

    private:
        // Private implementation.
        class Impl;

        std::unique_ptr<Impl> pImpl;

        static const XMMATRIX MatrixIdentity;
        static const XMFLOAT2 Float2Zero;

        // Prevent copying.
        SpriteBatch(SpriteBatch const&);
        SpriteBatch& operator= (SpriteBatch const&);
    };
}
