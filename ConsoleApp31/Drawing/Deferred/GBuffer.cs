using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31.Drawing.Deferred;
internal class GBuffer
{
    public RenderTexture Position;
    public RenderTexture Albedo;

    public GBuffer(int width, int height)
    {
        Resize(width, height);
    }

    [MemberNotNull(nameof(Position))]
    [MemberNotNull(nameof(Albedo))]
    public void Resize(int width, int height)
    {
        Position?.Dispose();
        Position = new(width, height, Format.R32G32B32A32_Float);

        Albedo?.Dispose();
        Albedo = new(width, height, Format.R8G8B8A8_UNorm);
    }

    public void Clear(ID3D11DeviceContext context)
    {
        context.ClearRenderTargetView(Position.RenderTargetView, new(new Vector4(float.PositiveInfinity)));
        context.ClearRenderTargetView(Albedo.RenderTargetView, new(0x8F, 0xD9, 0xEA));
    }

    public void ApplyRenderTargets(ID3D11DeviceContext context, ID3D11DepthStencilView? depthStencilView = null)
    {
        context.OMSetRenderTargets(GetRenderTargetViewArray(), depthStencilView);
    }

    private ID3D11RenderTargetView[] GetRenderTargetViewArray()
    {
        return new[] 
        { 
            Position.RenderTargetView, 
            Albedo.RenderTargetView 
        };
    }
}
