using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;

namespace ConsoleApp31.Drawing;
internal class Mesh : IDisposable
{
    public Material Material { get; set; }

    public VertexBuffer<VertexPositionTexture> Vertices { get; private set; }
    public IndexBuffer Indices { get; private set; }

    public Mesh(Span<VertexPositionTexture> vertices, Span<uint> indices, Material material)
    {
        this.Vertices = new(vertices);
        this.Indices = new(indices);

        Material = material;
    }

    public void Render(Camera camera, Transform transform)
    {
        Render(camera, transform.GetMatrix());
    }

    public void Render(Camera camera, Matrix4x4 transform)
    {
        if (Indices.Length is 0)
            return;

        var context = Graphics.ImmediateContext;

        Material.RenderSetup(context, camera, transform);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.SetVertexBuffer(Vertices);
        context.SetIndexBuffer(Indices);
        context.DrawIndexed(Indices.Length, 0, 0);
    }

    public void Dispose()
    {
        Vertices.Dispose();
        Indices.Dispose();
    }
}
