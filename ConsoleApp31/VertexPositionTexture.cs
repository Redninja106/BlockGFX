using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal struct VertexPositionTexture
{
    public Vector3 Position;
    public Vector2 TextureCoordinate;

    public VertexPositionTexture(Vector3 position, Vector2 textureCoordinate)
    {
        Position = position;
        TextureCoordinate = textureCoordinate;
    }
}
