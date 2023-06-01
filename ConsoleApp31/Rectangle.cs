using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal struct Rectangle
{
    public Vector2 Position;
    public Vector2 Size;

    public Rectangle(float x, float y, float width, float height) : this(new(x, y), new(width, height))
    {
    }

    public Rectangle(Vector2 position, Vector2 size)
    {
        Position = position;
        Size = size;
    }

    public void GetCorners(out Vector2 topLeft, out Vector2 topRight, out Vector2 bottomLeft, out Vector2 bottomRight)
    {
        topLeft = Position;
        topRight = Position + new Vector2(Size.X, 0);
        bottomLeft = Position + new Vector2(0, Size.Y);
        bottomRight = Position + Size;
    }
}
