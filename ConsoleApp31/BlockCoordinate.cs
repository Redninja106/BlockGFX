using System.Numerics;

namespace ConsoleApp31;

public struct BlockCoordinate
{
    public int X, Y, Z;

    public BlockCoordinate(Vector3 coordinate) : this((int)coordinate.X, (int)coordinate.Y, (int)coordinate.Z)
    {
    }

    public BlockCoordinate(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public ChunkCoordinate ToChunkCoordinate()
    {
        return new(
            (int)MathF.Floor(X / (float)BlockChunk.Width),
            (int)MathF.Floor(Y / (float)BlockChunk.Height),
            (int)MathF.Floor(Z / (float)BlockChunk.Depth)
            );
    }

    public BlockCoordinate OffsetBy(BlockCoordinate coordinate)
    {
        return this + new BlockCoordinate(coordinate.X, coordinate.Y, coordinate.Z);
    }

    internal Vector3 ToVector3()
    {
        return new(X, Y, Z);
    }

    public static BlockCoordinate operator +(BlockCoordinate a, BlockCoordinate b)
    {
        return new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }
    public static BlockCoordinate operator -(BlockCoordinate a, BlockCoordinate b)
    {
        return new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }
}