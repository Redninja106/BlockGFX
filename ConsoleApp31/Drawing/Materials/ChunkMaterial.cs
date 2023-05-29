using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing.Materials;
internal class ChunkMaterial : Material
{
    public ChunkMaterial(VertexShader vertexShader, PixelShader pixelShader, ImageTexture atlas, Sampler sampler) : base(vertexShader, pixelShader)
    {
        Atlas = atlas;
        Sampler = sampler;
    }

    public ImageTexture Atlas { get; set; }
    public Sampler Sampler { get; set; }

    public override void RenderSetup(ID3D11DeviceContext context, Camera camera, Matrix4x4 transform)
    {
        PixelShader.SamplerStates[0] = Sampler.State;
        PixelShader.ResourceViews[0] = Atlas.ShaderResourceView;

        base.RenderSetup(context, camera, transform);
    }
}
