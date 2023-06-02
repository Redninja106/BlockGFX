using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace ConsoleApp31.Drawing;
internal class Mesh : IDisposable
{
    public VertexBuffer<VertexPositionTexture> Vertices { get; private set; }
    public IndexBuffer? Indices { get; private set; }

    private static ID3D11RasterizerState? sharedWireframeRSState;

    /// <summary>
    /// Creats a new mesh. Pass <c>Span&lt;uint&gt;.Empty</c> to disable indexing.
    /// </summary>
    public Mesh(Span<VertexPositionTexture> vertices, Span<uint> indices)
    {
        this.Vertices = new(vertices);
        
        if (!indices.IsEmpty)
            this.Indices = new(indices);

        sharedWireframeRSState ??= Graphics.Device.CreateRasterizerState(new(CullMode.None, FillMode.Wireframe));
    }

    public void Render(Camera camera, Material? material, Transform transform, bool wireframe = false)
    {
        Render(camera, material, transform.GetMatrix(), wireframe);
    }

    public void Render(Camera camera, Material? material, Matrix4x4 transform, bool wireframe = false)
    {
        if (Indices is null && Vertices.Length is 0)
        {
            return;
        }
        else if (Indices?.Length is 0)
        {
            return;
        }

        var context = Graphics.ImmediateContext;

        material?.RenderSetup(context, camera, transform);

        if (wireframe)
        {
            context.RSSetState(sharedWireframeRSState);
        }
        else
        {
            context.RSSetState(null);
        }

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        InvokeDraw(context);
    }

    public void InvokeDraw(ID3D11DeviceContext context)
    {
        context.SetVertexBuffer(Vertices);

        if (Indices is null)
        {
            context.Draw(Vertices.Length, 0);
        }
        else
        {
            if (Indices.Length is 0)
                return;

            context.SetIndexBuffer(Indices);
            context.DrawIndexed(Indices.Length, 0, 0);
        }
    }

    public void Dispose()
    {
        Vertices.Dispose();
        Indices?.Dispose();
    }
}
