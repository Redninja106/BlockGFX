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
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace ConsoleApp31;
internal class BlockMeshBuilder
{
    private static readonly uint[] indices = new uint[] { 0, 2, 1, 1, 2, 3 };
    BlockVertex[] vertices = new BlockVertex[4];
    private int faceCount;
    private List<FaceInfo> faceInfos = new();

    public BlockChunkManager BlockChunkManager { get; }
    public TextureAtlas TextureAtlas { get; }

    public BlockMeshBuilder(TextureAtlas textureAtlas, BlockChunkManager blockChunkManager)
    {
        BlockChunkManager = blockChunkManager;
        this.TextureAtlas = textureAtlas;
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

    public BlockMesh BuildMesh(BlockChunk chunk, int width, int height, int depth)
    {
        MeshBuilder<BlockVertex> mb = new();
        BlockBoundingBoxBuilder bbBuilder = new();

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

        var boxes = bbBuilder.Build(chunk, width, height, depth);
        mb.Finish(out var vertices, out var indices);
        return new(vertices, indices, boxes, faceInfos.ToArray());
    }

    void CreateFaceIfNeeded(MeshBuilder<BlockVertex> mb, BlockCoordinate chunkOrigin, BlockCoordinate localCoordinate, BlockID id, Orientation side)
    {
        BlockCoordinate worldCoordinate = chunkOrigin + localCoordinate;

        if (BlockChunkManager.TryGetBlock(worldCoordinate.OffsetBy(new(side.GetNormal())), out BlockData blockData) && blockData.IsTransparent)
        {
            mb.Add(CreateFace(localCoordinate.ToVector3(), id, side));
        }
    }

    private MeshPart<BlockVertex> CreateFace(Vector3 offset, BlockID blockID, Orientation side)
    {
        var normal = side.GetNormal();

        Vector3 axis = normal.Y is not 0 ? Vector3.UnitZ : Vector3.UnitY;
        Vector3 perpAxis = Vector3.Cross(normal, axis);

        Rectangle bounds = TextureAtlas.GetTileBounds(blockID, side);
        bounds.GetCorners(out Vector2 topLeft, out Vector2 topRight, out Vector2 bottomLeft, out Vector2 bottomRight);

        uint faceIndex = (uint)faceCount++;
        faceInfos.Add(new()
        {
            position = offset,
            up = axis,
            right = perpAxis
        });

        vertices[0] = new(offset + .5f * (normal + axis + perpAxis), faceIndex, topLeft, new(1, 1));
        vertices[1] = new(offset + .5f * (normal - axis + perpAxis), faceIndex, bottomLeft, new(0, 1));
        vertices[2] = new(offset + .5f * (normal + axis - perpAxis), faceIndex, topRight, new(1, 0));
        vertices[3] = new(offset + .5f * (normal - axis - perpAxis), faceIndex, bottomRight, new(0, 0));
        
        return new MeshPart<BlockVertex>(vertices, indices);
    }

}

public struct BlockVertex
{
    // LATER: this can be compressed to save bandwidth

    public Vector3 position;
    public uint faceIndex;
    public Vector2 uv;
    public Vector2 localUV;

    public BlockVertex(Vector3 position, uint faceIndex, Vector2 uv, Vector2 localUv)
    {
        this.position = position;
        this.faceIndex = faceIndex;
        this.uv = uv;
        this.localUV = localUv;
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

class BlockMesh : IDisposable
{
    public VertexBuffer<BlockVertex> vertexBuffer;

    public IndexBuffer indexBuffer;

    public StructuredBuffer<FaceInfo> faceInfos;
    public StructuredBuffer<Box> hitBoxBuffer;
    public List<Box> hitBoxes;

    // highest bit of the alpha channel is visiblity flag from first rasterizer stage
    public UnorderedAccessTexture? faces;

    public BlockMesh(Span<BlockVertex> vertices, Span<uint> indices, List<Box> hitBoxes, FaceInfo[] faceInfos)
    {
        // render uids to render texture

        // determine faces we have to raycast against ??? how????
        // pass texture index all the way to ps and treat highest bit of the alpha component of the faces texture as a visibility flag!

        // CS invocation: raycast once per visible block pixel within a certain distance (maybe with lods) into this chunk's face texture

        // render chunks with uvs, textured by our face texture.

        vertexBuffer = new(vertices);
        indexBuffer = new(indices);

        this.hitBoxes = hitBoxes;

        var boxes = CollectionsMarshal.AsSpan(hitBoxes);
        this.hitBoxBuffer = new(boxes);

        this.faceInfos = new(faceInfos);

        if (faceInfos.Length != 0)
        {
            faces = new(16 * faceInfos.Length, 16, Format.R8G8B8A8_UNorm, Format.R8G8B8A8_Typeless);
        }
    }

    public void Render(Camera camera, Material material, Matrix4x4 transformationMatrix)
    {
        if (faces is null)
            return;

        var context = Graphics.ImmediateContext;
        material.RenderSetup(context, camera, transformationMatrix);

        InvokeDraw(context);
    }

    public void InvokeDraw(ID3D11DeviceContext context)
    {
        if (faces is null)
            return;

        context.SetVertexBuffer(vertexBuffer);
        context.SetIndexBuffer(indexBuffer);

        context.DrawIndexed(indexBuffer.Length, 0, 0);
    }

    public void Dispose()
    {
        vertexBuffer.Dispose();
        indexBuffer.Dispose();
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
