using ConsoleApp31.Drawing.Materials;
using ConsoleApp31.Texturing;
using GLFW;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.Mathematics;

namespace ConsoleApp31;
internal class BlockChunkManager : IGameComponent, ICollidable
{
    public ChunkMaterial SharedMaterial { get; }
    public Dictionary<ChunkCoordinate, BlockChunk> Chunks { get; } = new();
    public TextureAtlas TextureAtlas { get; }

    public BlockChunkManager()
    {
        var grassfaces = new BlockFaces()
        {
            Forward = "Assets/grass_block_side.png",
            Backward = "Assets/grass_block_side.png",
            Left = "Assets/grass_block_side.png",
            Right = "Assets/grass_block_side.png",
            Top = "Assets/grass_block_top.png",
            Bottom = "Assets/dirt.png"
        };

        var cobbleFaces = new BlockFaces("Assets/cobblestone.png");
        var dirtFaces = new BlockFaces("Assets/dirt.png");
        var stoneFaces = new BlockFaces("Assets/stone.png");
        var bedrockFaces = new BlockFaces("Assets/bedrock.png");
        var glowstoneFaces = new BlockFaces("Assets/glowstone.png");

        var atlasBuilder = new TextureAtlasBuilder();

        atlasBuilder.AddBlock(new(1), grassfaces);
        atlasBuilder.AddBlock(new(2), dirtFaces);
        atlasBuilder.AddBlock(new(3), cobbleFaces);
        atlasBuilder.AddBlock(new(4), stoneFaces);
        atlasBuilder.AddBlock(new(5), bedrockFaces);
        atlasBuilder.AddBlock(new(6), glowstoneFaces);

        TextureAtlas = atlasBuilder.Finish();

        AddChunk(new(0, 0, 0));
    }

    public void Render(Camera camera)
    {
    }

    public void Update(float dt)
    {
    }

    public bool Intersect(Box box, out Box overlap)
    {
        var center = (box.min + box.max) * .5f;
        var dist = Vector3.Distance(box.min, box.max);

        var colliders = GetNearbyColliders(new(center), dist);

        foreach (var collider in colliders)
        {
            if (collider?.Intersect(box, out overlap) ?? false)
            {
                return true;
            }
        }

        overlap = default;
        return false;
    }

    public void AddChunk(ChunkCoordinate coordinate)
    {
        BlockChunk c = new(this, coordinate);
        Program.World.Add(c);
        Chunks.Add(coordinate, c);
        c.Rebuild();
    }

    public void RemoveChunk(ChunkCoordinate coordinate)
    {
        BlockChunk? chunk = GetChunk(coordinate);

        if (chunk is null)
            return;

        Program.World.Remove(chunk);
        Chunks.Remove(coordinate);
    }

    private BlockChunk? getBlockLastChunk;

    public bool TryGetBlock(BlockCoordinate worldCoordinate, out BlockData value)
    {
        BlockChunk? chunk;

        if (getBlockLastChunk?.location.Contains(worldCoordinate) ?? false)
        {
            chunk = getBlockLastChunk;
        }
        else
        {
            chunk = GetChunk(worldCoordinate.ToChunkCoordinate());
        }

        if (chunk is null)
        {
            value = default;
            return false;
        }

        getBlockLastChunk = chunk;
        value = chunk[worldCoordinate];
        return true;
    }

    public bool TrySetBlock(BlockCoordinate worldCoordinate, BlockData value)
    {
        var chunk = GetChunk(worldCoordinate.ToChunkCoordinate());

        if (chunk is null)
        {
            return false;
        }

        chunk[worldCoordinate] = value;


        chunk.Rebuild();

        var localCoordinate = chunk.ToLocal(worldCoordinate);

        if (localCoordinate.X is 0)
            chunk.GetNeighbor(Orientation.Left)?.Rebuild();

        if (localCoordinate.X is BlockChunk.Width - 1)
            chunk.GetNeighbor(Orientation.Right)?.Rebuild();

        if (localCoordinate.Y is 0)
            chunk.GetNeighbor(Orientation.Bottom)?.Rebuild();

        if (localCoordinate.Y is BlockChunk.Height - 1)
            chunk.GetNeighbor(Orientation.Top)?.Rebuild();

        if (localCoordinate.Z is 0)
            chunk.GetNeighbor(Orientation.Backward)?.Rebuild();

        if (localCoordinate.Z is BlockChunk.Depth - 1)
            chunk.GetNeighbor(Orientation.Forward)?.Rebuild();


        return true;
    }

    public bool Raycast(Ray ray, out RaycastHit hit)
    {
        hit = RaycastHit.NoHit;

        BlockCoordinate origin = new(ray.origin);

        using RentedArray<ChunkCollider?> colliders = GetNearbyColliders(origin, ray.length);

        foreach (var collider in colliders)
        {
            if (collider is not null && collider.Raycast2(ray, out var lastHit) && lastHit.T < hit.T)
            {
                hit = lastHit;
            }
        }

        return hit.Hit;
    }


    private RentedArray<ChunkCollider?> GetNearbyColliders(BlockCoordinate location, float radius)
    {
        var result = new RentedArray<ChunkCollider?>(3 * 3 * 3);

        var baseLocation = location.ToChunkCoordinate();

        int index = 0;
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var chunkLocation = new ChunkCoordinate(baseLocation.X + x, baseLocation.Y + y, baseLocation.Z + z);

                    var collider = GetChunk(chunkLocation)?.Collider;

                    if (collider is not null)
                    {
                        result[index++] = collider;
                    }
                }
            }
        }

        return result;
    }

    public BlockChunk? GetChunk(ChunkCoordinate coordinate)
    {
        return Chunks.GetValueOrDefault(coordinate);
    }

}

public record struct ChunkCoordinate
{
    public int X, Y, Z;

    public ChunkCoordinate(Vector3 vector) : this((int)vector.X, (int)vector.Y, (int)vector.Z)
    {

    }

    public ChunkCoordinate(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public readonly BlockCoordinate ToBlockCoordinate()
    {
        return new BlockCoordinate(
            X * BlockChunk.Width, 
            Y * BlockChunk.Height, 
            Z * BlockChunk.Depth
            );
    }

    public Vector3 ToVector3()
    {
        return new(X, Y, Z);
    }

    public ChunkCoordinate OffsetBy(ChunkCoordinate coordinate)
    {
        return new(this.X + coordinate.X, this.Y + coordinate.Y, this.Z + coordinate.Z);
    }

    public bool Contains(BlockCoordinate block)
    {
        return this == block.ToChunkCoordinate();
    }
}

class ChunkCollider : ICollidable
{
    private Vector3 globalOffset;
    private ChunkCoordinate location;
    private List<Box> colliders;
    private BlockData[] blocks;
    public ChunkCollider(ChunkCoordinate location, BlockData[] blocks, List<Box> colliders)
    {
        this.globalOffset = -location.ToBlockCoordinate().ToVector3();
        this.colliders = colliders;
        this.blocks = blocks;
    }

    public bool Intersect(Box box, out Box overlap)
    {
        // TODO: return overlap in global space

        box.min += globalOffset;
        box.max += globalOffset;

        var pos = location.ToBlockCoordinate().ToVector3();
        var chunkCollider = new Box(pos, pos + Vector3.One * 16);

        if (!chunkCollider.Intersect(box, out _))
        {
            overlap = default;
            return false;
        }

        overlap = default;

        for (int i = 0; i < colliders.Count; i++)
        {
            if (colliders[i].Intersect(box, out overlap))
                return true;
        }

        return false;
    }

    public bool Raycast(Ray ray, out RaycastHit hit)
    {
        ray.origin += globalOffset;

        hit = RaycastHit.NoHit;

        for (int i = 0; i < colliders.Count; i++)
        {
            if (colliders[i].Raycast(ray, out var lastHit))
            {
                if (lastHit.T < hit.T)
                {
                    hit = lastHit;
                }
            }
        }

        var box = hit.box;
        
        box.min -= globalOffset;
        box.max -= globalOffset;

        hit = new(hit.T, hit.normal, box);

        return hit.Hit;
    }


    public bool Raycast2(Ray ray, out RaycastHit hit)
    {
        hit = default;

        // ray.origin -= Vector3.One * .5f;

        ray.direction = ray.direction.Normalized();

        Vector3 voxel = new((int)MathF.Floor(ray.origin.X), (int)MathF.Floor(ray.origin.Y), (int)MathF.Floor(ray.origin.Z));
        Vector3 step = new(MathF.Sign(ray.direction.X), MathF.Sign(ray.direction.Y), MathF.Sign(ray.direction.Z));

        if (step.LengthSquared() == 0)
            return false;

        float tNear, tFar;
        Box box;
        box.min = new Vector3(0, 0, 0);
        box.max = new Vector3(16, 16, 16);
        box.PartialRaycast(ray, out tNear, out tFar);

        Vector3 start = ray.At(MathF.Max(0, tNear));
        Vector3 end = ray.At(tFar);

        Vector3 d = end - start;

        Vector3 tDelta = step / d;

        Vector3 tMax = tDelta * new Vector3(Frac(start.X, step.X), Frac(start.Y, step.Y), Frac(start.Z, step.Z));

        float Frac(float f,float s)
        {
            if (s > 0)
                return 1 - f + MathF.Floor(f);
            else
                return f - MathF.Floor(f);
        }
        
        Vector3 normal;

        while (true)
        {
            if (step.X is not 0 && tMax.X < tMax.Y)
            {
                if (step.X is not 0 && tMax.X < tMax.Z)
                {
                    voxel.X += step.X;
                    tMax.X += tDelta.X;
                    normal = Vector3.UnitX * -step.X;
                }
                else
                {
                    voxel.Z += step.Z;
                    tMax.Z += tDelta.Z;
                    normal = Vector3.UnitZ * -step.Z;
                }
            }
            else
            {
                if (step.Y is not 0 && tMax.Y < tMax.Z)
                {
                    voxel.Y += step.Y;
                    tMax.Y += tDelta.Y;
                    normal = Vector3.UnitY * -step.Y;
                }
                else
                {
                    voxel.Z += step.Z;
                    tMax.Z += tDelta.Z;
                    normal = Vector3.UnitZ * -step.Z;
                }
            }

            if (voxel.X < 0 || voxel.X >= 16 || voxel.Y < 0 || voxel.Y >= 16 || voxel.Z < 0 || voxel.Z >= 16)
            {
                return false;
            }

            if (!blocks[(int)(voxel.Y * 16 * 16 + voxel.X * 16 + voxel.Z)].IsTransparent)
            {
                hit = new(0, normal, new(voxel, voxel + Vector3.One));


                return true;
            }

        }

        // return voxel.X < 0 || voxel.X >= 16 || voxel.Y < 0 || voxel.Y >= 16 || voxel.Z < 0 || voxel.Z >= 16;
    }
}

interface ICollidable
{
    bool Raycast(Ray ray, out RaycastHit hit);
    bool Intersect(Box box, out Box overlap);
}

struct Ray
{
    public Vector3 origin;
    public Vector3 direction;
    public Vector3 inverseDirection;
    public float length;

    public Ray(Transform transform, float length) : this(transform.Position, transform.Forward, length)
    {

    }

    public Ray(Vector3 origin, Vector3 direction, float length) : this()
    {
        this.origin = origin;
        this.direction = direction;
        this.inverseDirection = Vector3.One / direction;
        this.length = length;
    }

    public Vector3 At(float t)
    {
        return origin + direction * t;
    }
}

readonly record struct RaycastHit
{
    public static readonly RaycastHit NoHit = new(float.PositiveInfinity, Vector3.Zero, default);

    public readonly float T;
    public readonly Vector3 normal;
    public readonly Box box;

    public bool Hit => T != float.PositiveInfinity;

    public RaycastHit(float t, Vector3 normal, Box box)
    {
        T = t;
        this.normal = normal;
        this.box = box;
    }
}

readonly struct RentedArray<T> : IDisposable, IEnumerable<T>
{
    private readonly T[] elements;

    public int Length => elements.Length;

    public RentedArray(int minimumLength)
    {
        elements = ArrayPool<T>.Shared.Rent(minimumLength);
    }

    public void Return(bool clearArray = false)
    {
        if (elements is not null)
            ArrayPool<T>.Shared.Return(elements, clearArray);
    }

    void IDisposable.Dispose()
    {
        Return();
    }

    public static T[] GetArray(RentedArray<T> array)
    {
        return array.elements;
    }
    
    public static void Resize(ref RentedArray<T> array, int newMinimumLength)
    {
        if (newMinimumLength < array.Length)
        {
            // shrinking

        }
    }

    public IEnumerator<T> GetEnumerator() => (elements as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public readonly Span<T> AsSpan() => this.elements.AsSpan();
    public ref T this[int index] => ref this.elements[index];
}