using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace ConsoleApp31.GUI;
internal class ElementRenderer
{
    private static readonly VertexPositionTexture[] quadVertices = new VertexPositionTexture[]
    {
        new(new(0, 0, 0), new(0, 0)),
        new(new(0, 1, 0), new(0, 1)),
        new(new(1, 0, 0), new(1, 0)),
        
        new(new(1, 0, 0), new(1, 0)),
        new(new(0, 1, 0), new(0, 1)),
        new(new(1, 1, 0), new(1, 1))
    };

    private VertexShader guiVertexShader = new("gui_vs.hlsl");
    private PixelShader guiPixelShader = new("gui_ps.hlsl");
    private VertexBuffer<VertexPositionTexture> quadBuffer = new(quadVertices);
    private ConstantBuffer<RenderData> dataBuffer = new(default);
    private Sampler sampler = new(Vortice.Direct3D11.Filter.MinMagMipPoint, Vortice.Direct3D11.TextureAddressMode.Wrap);
    private ID3D11BlendState blendState;

    public ElementRenderer()
    {
        BlendDescription desc = new(Blend.SourceAlpha, Blend.InverseSourceAlpha);

        blendState = Graphics.Device.CreateBlendState(desc);
    }

    public void DrawTexture(Rectangle src, Rectangle dest, ImageTexture image)
    {
        DrawTexture(src, dest, image.ShaderResourceView);
    }

    public void DrawTexture(Rectangle src, Rectangle dest, ID3D11ShaderResourceView imageView)
    {
        dataBuffer.Update(new()
        {
            aspectRatio = Program.Camera.AspectRatio,
            dest = dest,
            source = src,
        });

        var context = Graphics.ImmediateContext;

        guiVertexShader.ConstantBuffers[0] = dataBuffer.InternalBuffer;
        context.SetVertexShader(guiVertexShader);

        guiPixelShader.SamplerStates[0] = sampler.State;
        guiPixelShader.ResourceViews[0] = imageView;
        context.SetPixelShader(guiPixelShader);
        
        context.SetVertexBuffer(quadBuffer);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.OMSetBlendState(this.blendState);
        
        context.Draw(6, 0);
    }

    struct RenderData
    {
        public Rectangle source;
        public Rectangle dest;
        public float aspectRatio;
    }
}
