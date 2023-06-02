using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31.Drawing;
internal static class PrimitiveMeshes
{
    // 1x1 xy quad at (0, 0) z=0
    public static Mesh Quad { get; private set; }

    // radius 1 sphere at (0,0,0)
    public static Mesh Sphere { get; private set; }


    public static void Initialize()
    {
        var quadVerts = new VertexPositionTexture[]
        {
            new(new(0, 0, 0), new(0, 0)),
            new(new(0, 1, 0), new(0, 1)),
            new(new(1, 0, 0), new(1, 0)),

            new(new(1, 0, 0), new(1, 0)),
            new(new(0, 1, 0), new(0, 1)),
            new(new(1, 1, 0), new(1, 1))
        };

        Quad = new Mesh(quadVerts, Span<uint>.Empty);

        GenerateSphereMesh();
    }

    private static void GenerateSphereMesh()
    {
        const int verticalVertices = 16;
        const int horizontalVertices = 32;

        var vertices = new VertexPositionTexture[verticalVertices * horizontalVertices];

        for (int i = 0; i < verticalVertices; i++)
        {
            float iAngle = (i / (float)(verticalVertices - 1)) * MathF.PI - (MathF.PI/2f);
            float radius = MathF.Cos(iAngle);
            float y = MathF.Sin(iAngle);

            for (int j = 0; j < horizontalVertices; j++)
            {
                float jAngle = (j / (float)(horizontalVertices - 1)) * MathF.Tau;
                float x = radius * MathF.Cos(jAngle);
                float z = radius * MathF.Sin(jAngle);

                vertices[i * horizontalVertices + j] = new VertexPositionTexture(new(x, y, z), new(jAngle * (1f / MathF.Tau), y));
            }
        }

        var indices = new uint[6 * (verticalVertices - 1) * horizontalVertices];
        int indicesOffset = 0;

        for (int i = 0; i < verticalVertices - 1; i++)
        {
            for (int j = 0; j < horizontalVertices; j++)
            {
                int nextJ = (j + 1) % horizontalVertices;

                indices[indicesOffset++] = (uint)(i * horizontalVertices + j);
                indices[indicesOffset++] = (uint)(i * horizontalVertices + nextJ);
                indices[indicesOffset++] = (uint)((i + 1) * horizontalVertices + j);

                indices[indicesOffset++] = (uint)(i * horizontalVertices + nextJ);
                indices[indicesOffset++] = (uint)((i + 1) * horizontalVertices + nextJ);
                indices[indicesOffset++] = (uint)((i + 1) * horizontalVertices + j);
            }
        }

        Sphere = new(vertices, indices);
    }
}
