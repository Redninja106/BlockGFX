using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;
internal class Material
{
    public VertexShader VertexShader { get; }
    public PixelShader PixelShader { get; }

    public ConstantBuffer<MatrixBufferData> MatrixBuffer { get; }

    public Material(VertexShader vertexShader, PixelShader pixelShader)
    {
        VertexShader = vertexShader;
        PixelShader = pixelShader;

        MatrixBuffer = new();
    }

    public virtual void RenderSetup(ID3D11DeviceContext context, Camera camera, Matrix4x4 transform)
    {
        MatrixBuffer.Update(new(transform, camera));
        VertexShader.ConstantBuffers[0] = MatrixBuffer.InternalBuffer;
        context.SetVertexShader(this.VertexShader);

        context.SetPixelShader(this.PixelShader);
    }
}