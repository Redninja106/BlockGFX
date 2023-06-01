using ConsoleApp31.Texturing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp31;
internal class BlockMeshBuilder
{
    private static readonly uint[] indices = new uint[] { 0, 2, 1, 1, 2, 3 };
    VertexPositionTexture[] vertices = new VertexPositionTexture[4];

    public TextureAtlas TextureAtlas { get; }
    public BlockChunkManager BlockChunkManager { get; }
    public BlockMeshBuilder(TextureAtlas textureAtlas, BlockChunkManager blockChunkManager)
    {
        TextureAtlas = textureAtlas;
        BlockChunkManager = blockChunkManager;
    }

    public static readonly Orientation[] orientations = new[]
    {
        Orientation.Top,
        Orientation.Bottom,
        Orientation.Forward,
        Orientation.Right,
        Orientation.Backward,
        Orientation.Left
    };

    public MeshBuilder<VertexPositionTexture> BuildMesh(BlockChunk chunk, int width, int height, int depth)
    {
        MeshBuilder<VertexPositionTexture> mb = new();

        BlockCoordinate blockOrigin = chunk.location.ToBlockCoordinate();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    BlockCoordinate localCoordinate = new(x, y, z);

                    var block = chunk[x, y, z];

                    if (block.IsTransparent)
                    {
                        continue;
                    }

                    for (int i = 0; i < orientations.Length; i++)
                    {
                        CreateFaceIfNeeded(mb, blockOrigin, localCoordinate, block.ID, orientations[i]);
                    }
                }
            }
        }

        return mb;
    }

    void CreateFaceIfNeeded(MeshBuilder<VertexPositionTexture> mb, BlockCoordinate chunkOrigin, BlockCoordinate localCoordinate, BlockID id, Orientation side)
    {
        BlockCoordinate worldCoordinate = chunkOrigin + localCoordinate;

        if (BlockChunkManager.TryGetBlock(worldCoordinate.OffsetBy(new(side.GetNormal())), out BlockData blockData) && blockData.IsTransparent)
        {
            mb.Add(CreateFace(localCoordinate.ToVector3(), id, side));
        }
    }

    private MeshPart<VertexPositionTexture> CreateFace(Vector3 offset, BlockID id, Orientation side)
    {
        var normal = side.GetNormal();

        Vector3 axis = normal.Y is not 0 ? Vector3.UnitZ : Vector3.UnitY;
        Vector3 perpAxis = Vector3.Cross(normal, axis);

        Rectangle bounds = TextureAtlas.GetTileBounds(id, side);

        bounds.GetCorners(out Vector2 topLeft, out Vector2 topRight, out Vector2 bottomLeft, out Vector2 bottomRight);
        
        vertices[0] = new(offset + .5f * (normal + axis + perpAxis), topLeft);
        vertices[1] = new(offset + .5f * (normal - axis + perpAxis), bottomLeft);
        vertices[2] = new(offset + .5f * (normal + axis - perpAxis), topRight);
        vertices[3] = new(offset + .5f * (normal - axis - perpAxis), bottomRight);
        
        return new MeshPart<VertexPositionTexture>(vertices, indices);
    }
}

class MeshBuilder<TVertex> where TVertex : unmanaged
{
    private readonly List<TVertex> vertices = new();
    private readonly List<uint> indices = new();

    private bool finished = false;

    public MeshBuilder()
    {

    }

    public void Add(MeshPart<TVertex> meshPart)
    {
        if (finished)
            throw new InvalidOperationException();

        uint indexOffset = (uint)vertices.Count;

        foreach (var vertex in meshPart.Vertices)
        {
            vertices.Add(vertex);
        }

        foreach (var localIndex in meshPart.LocalIndices)
        {
            indices.Add(localIndex + indexOffset);
        }
    }

    public void Finish(out Span<TVertex> vertices, out Span<uint> indices)
    {
        finished = true;

        vertices = CollectionsMarshal.AsSpan(this.vertices);
        indices = CollectionsMarshal.AsSpan(this.indices);
    }
}

ref struct MeshPart<TVertex> where TVertex : unmanaged
{
    public Span<TVertex> Vertices;
    public Span<uint> LocalIndices;

    public MeshPart(Span<TVertex> vertices, Span<uint> localIndices)
    {
        Vertices = vertices;
        LocalIndices = localIndices;
    }
}
