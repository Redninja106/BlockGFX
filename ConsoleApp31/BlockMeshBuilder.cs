using ConsoleApp31.Texturing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace ConsoleApp31;
internal class BlockMeshBuilder
{
    private static readonly uint[] indices = new uint[] { 0, 2, 1, 1, 2, 3 };
    BlockVertex[] vertices = new BlockVertex[4];
    private int faceCount;
    private List<FaceInfo> faceInfos = new();

    public BlockChunkManager BlockChunkManager { get; }
    public TextureAtlas TextureAtlas { get; }

    private List<Light> lights = new();

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

        mb.Finish(out var vertices, out var indices);
        return new(vertices, indices, lights, faceInfos.ToArray());
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

        Vector3 up = normal.Y is not 0 ? Vector3.UnitZ : Vector3.UnitY;
        Vector3 right = Vector3.Cross(normal, up);

        Rectangle bounds = TextureAtlas.GetTileBounds(blockID, side);
        bounds.GetCorners(out Vector2 topLeft, out Vector2 topRight, out Vector2 bottomLeft, out Vector2 bottomRight);

        uint faceIndex = (uint)faceCount++;

        TextureAtlas.GetTileLocation(blockID, side, out uint x, out uint y);

        faceInfos.Add(new()
        {
            position = offset,
            up = up,
            right = right,
            atlasLocationX = x,
            atlasLocationY = y
        });

        if (blockID.Value == 6)
        {
            Vector3 pos = offset + .5f * Vector3.One;
            BlockCoordinate blockCoord = new(offset);

            bool topTransparent = false, bottomTransparent = false, leftTransparent = false, rightTransparent = false;
            BlockData block;

            if (BlockChunkManager.TryGetBlock(blockCoord.OffsetBy(new(up)), out block))
                topTransparent = block.IsTransparent;
            if (BlockChunkManager.TryGetBlock(blockCoord.OffsetBy(new(-up)), out block))
                bottomTransparent = block.IsTransparent;
            if (BlockChunkManager.TryGetBlock(blockCoord.OffsetBy(new(-right)), out block))
                leftTransparent = block.IsTransparent;
            if (BlockChunkManager.TryGetBlock(blockCoord.OffsetBy(new(right)), out block))
                rightTransparent = block.IsTransparent;

            var l = Light.CreateForFace(pos, normal, up, right, topTransparent, bottomTransparent, leftTransparent, rightTransparent);
            this.lights.Add(l);
        }

        float e = 0.0005f;

        vertices[0] = new(offset + .5f * (normal + up + right), faceIndex, topLeft, new(e, e));
        vertices[1] = new(offset + .5f * (normal - up + right), faceIndex, bottomLeft, new(e, 1-e));
        vertices[2] = new(offset + .5f * (normal + up - right), faceIndex, topRight, new(1-e, e));
        vertices[3] = new(offset + .5f * (normal - up - right), faceIndex, bottomRight, new(1-e, 1-e));
        
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

struct Light
{
    public Matrix4x4 projection;
    public Vector3 position;
    public Vector3 normal;
    public Vector3 up;
    public Vector3 right;

    public bool Test(Vector3 position)
    {
        Vector4 clip = Vector4.Transform(new Vector4(position, 1), this.projection);
        return MathF.Abs(clip.X) <= clip.W && MathF.Abs(clip.Y) <= clip.W && MathF.Abs(clip.Z) <= clip.W;
    }

    public static Light CreateForFace(Vector3 facePosition, Vector3 faceNormal, Vector3 faceUp, Vector3 faceRight, bool topTransparent, bool bottomTransparent, bool leftTransparent, bool rightTransparent)
    {
        float near = 0.001f;
        float far = 100f;

        float scale = 1.00001f;
        float extensionAmount = 5;

        float left = near * (leftTransparent ? -1 : -extensionAmount);
        float right = scale * near * (rightTransparent ? 1 : extensionAmount);
        float bottom = near * (bottomTransparent ? -1 : -extensionAmount);
        float top = scale * near * (topTransparent ? 1 : extensionAmount);

        return new()
        {
            projection = Matrix4x4.CreateLookAt(facePosition, facePosition + faceNormal, faceUp) * Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, near, far),
            position = facePosition,
            normal = faceNormal,
            up = faceUp,
            right = faceRight,
        };
    }
}

class BlockMesh : IDisposable
{
    public VertexBuffer<BlockVertex> vertexBuffer;

    public IndexBuffer indexBuffer;

    public StructuredBuffer<FaceInfo> faceInfos;
    public StructuredBuffer<Light>? lightBuffer;
    public List<Light> lights;

    // highest bit of the alpha channel is visiblity flag from first rasterizer pass
    public UnorderedAccessTexture? faces;
    public uint age;

    public BlockMesh(Span<BlockVertex> vertices, Span<uint> indices, List<Light> lights, FaceInfo[] faceInfos)
    {
        // render uids to render texture

        // determine faces we have to raycast against ??? how????
        // pass texture index all the way to ps and treat highest bit of the alpha component of the faces texture as a visibility flag!

        // CS invocation: raycast once per visible block pixel within a certain distance (maybe with lods) into this chunk's face texture

        // render chunks with uvs, textured by our face texture.

        vertexBuffer = new(vertices);
        indexBuffer = new(indices);

        this.lights = lights;

        if (lights.Count > 0)
        {
            var l = CollectionsMarshal.AsSpan(lights);
            this.lightBuffer = new(l);
        }

        this.faceInfos = new(faceInfos);

        if (faceInfos.Length != 0)
        {
            faces = new(16 * Math.Min(64, faceInfos.Length), 16 * (faceInfos.Length/64 + 1), Format.R8G8B8A8_UNorm, Format.R8G8B8A8_Typeless);
            // faces = new(16 * faceInfos.Length, 16, Format.R8G8B8A8_UNorm, Format.R8G8B8A8_Typeless);
        }
    }

    public void Update()
    {
        age++;
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
