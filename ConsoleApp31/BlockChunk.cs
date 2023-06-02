using ConsoleApp31.Drawing;
using GLFW;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Mail;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Vortice.Mathematics;

namespace ConsoleApp31;
internal class BlockChunk : IGameComponent, IDisposable
{
    public const int Width = 16, Height = 16, Depth = 16;

    public Transform Transform = new();
    readonly BlockData[] blocks = new BlockData[Width * Height * Depth];
    readonly BlockChunkManager manager;
    public ChunkCollider Collider;
    Mesh Mesh;

    public ChunkCoordinate location;

    public static Vector3 SizeVector => new(Width, Height, Depth);

    public BlockChunk(BlockChunkManager manager, ChunkCoordinate location)
    {
        this.location = location;
        this.Transform.Translate(location.ToBlockCoordinate().ToVector3() + Vector3.One * .5f);

        this.manager = manager;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    var coords = new BlockCoordinate(x, y, z) + location.ToBlockCoordinate();

                    this[x, y, z] = coords.Y switch
                    {
                        < -64 => new(5),
                        < 0 => new(4),
                        < 3 => new(2),
                        < 4 => new(1),
                        _ => new(0)
                    };
                }
            }
        }
    }

    public void Render(Camera camera)
    {
        Mesh.Render(camera, this.manager.SharedMaterial, Transform);
    }

    public void Update(float dt)
    {
    }

    public void Rebuild()
    {
        var builder = new BlockMeshBuilder(this.manager.TextureAtlas, this.manager);
        var meshBuilder = builder.BuildMesh(this, Width, Height, Depth);
        meshBuilder.Finish(out var verts, out var inds);
        Mesh = new Mesh(verts, inds);

        Collider = new(this.location, new BlockBoundingBoxBuilder().Build(this.blocks, Width, Height, Depth));
    }

    public ref BlockData this[int index]
    {
        get => ref blocks[index];
    }

    public ref BlockData this[int x, int y, int z]
    {
        get
        {
            // if (!IndexInBounds(x, y, z))
            //     throw new IndexOutOfRangeException();

            return ref blocks[y * Width * Depth + x * Depth + z]; 
        }
    }
    public ref BlockData this[BlockCoordinate worldCoordinate]
    {
        get
        {
            var localCoordinates = ToLocal(worldCoordinate);
            return ref this[localCoordinates.X, localCoordinates.Y, localCoordinates.Z];
        }
    }

    public static bool IndexInBounds(int x, int y, int z)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;
    }

    public BlockChunk? GetNeighbor(Orientation side)
    {
        return manager.GetChunk(this.location.OffsetBy(new(side.GetNormal())));
    }

    public BlockCoordinate ToLocal(BlockCoordinate worldCoordinate)
    {
        return worldCoordinate - this.location.ToBlockCoordinate();
    }

    public void Dispose()
    {
        Mesh.Dispose();
    }
}

//static class Ext
//{
//    public static bool Is<T>(this object obj) where T : class
//    {
//        return obj.Is<T>(out _);
//    }

//    public static bool Is<T>(this object obj, [NotNullWhen(true)] out T? t) where T : class
//    {
//        if (obj is T or IProxy<T>)
//        {
//            t = obj.As<T>()!;
//            return true;
//        }
        
//        t = null;
//        return false;
//    }

//    public static T? As<T>(this object obj) where T : class
//    {
//        return obj as T ?? (obj as IProxy<T>)?.GetObject();
//    }
//}

//interface IProxy<T> where T : class
//{
//    T GetObject();
//}