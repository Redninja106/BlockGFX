using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing.Deferred;
internal class DeferredMaterial : Material
{
    public GBuffer GBuffer { get; }
    public ConstantBuffer<GBufferConstantData> ConstantData { get; }

    public DeferredMaterial(GBuffer gBuffer, ConstantBuffer<GBufferConstantData> constantData, VertexShader vertexShader, PixelShader pixelShader) : base(vertexShader, pixelShader)
    {
        this.GBuffer = gBuffer;
        this.ConstantData = constantData;
    }

    public override void RenderSetup(ID3D11DeviceContext context, Camera camera, Matrix4x4 transform)
    {
        this.PixelShader.ResourceViews[0] = GBuffer.Position.ShaderResourceView;
        this.PixelShader.ResourceViews[1] = GBuffer.Albedo.ShaderResourceView;

        this.PixelShader.SamplerStates[0] = Sampler.PointWrap.State;

        this.PixelShader.ConstantBuffers[0] = ConstantData.InternalBuffer;

        base.RenderSetup(context, camera, transform);
    }
}
