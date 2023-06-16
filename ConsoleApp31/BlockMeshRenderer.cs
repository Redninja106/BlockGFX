using ConsoleApp31.Drawing.Materials;
using GLFW;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Vortice;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

class BlockMeshRenderer
{
    private DepthStencilTexture depthStencilTarget;
    private RenderTexture faceIndexTarget;
    private ChunkMaterial colorMaterial;
    private BlockChunkManager chunkManager;
    private Material depthOnlyMaterial;
    private Material faceVisibilityMaterial;

    private ID3D11DepthStencilState depthPassDSState;
    private ID3D11DepthStencilState postDepthPassDSState;
    private ComputeShader raytracingShader;
    private ConstantBuffer<RaytracingConstants> rtConsts;

    public BlockMeshRenderer(BlockChunkManager chunkManager)
    {
        this.chunkManager = chunkManager;
        depthStencilTarget = new DepthStencilTexture(Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);

        Graphics.AfterResize += () =>
        {
            depthStencilTarget?.Dispose();
            depthStencilTarget = new(Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);

            faceIndexTarget?.Dispose();
            faceIndexTarget = new(Graphics.RenderTargetWidth, Graphics.RenderTargetHeight, Format.R32_UInt);
        };

        VertexShader blockmeshVS = new("Shaders/blockmesh_vs.hlsl");

        colorMaterial = new(
            blockmeshVS,
            new("Shaders/blockmesh_color_ps.hlsl"),
            chunkManager.TextureAtlas.Texture,
            new(Filter.MinMagMipPoint, TextureAddressMode.Clamp)
            );

        depthOnlyMaterial = new(blockmeshVS, null);

        faceVisibilityMaterial = new(blockmeshVS, new("blockmesh_facevisibility_ps.hlsl"));

        depthPassDSState = Graphics.Device.CreateDepthStencilState(new(true, DepthWriteMask.All, ComparisonFunction.Less));
        postDepthPassDSState = Graphics.Device.CreateDepthStencilState(new(true, DepthWriteMask.Zero, ComparisonFunction.LessEqual));

        raytracingShader = new("blockmesh_raytracing_cs.hlsl");
        rtConsts = new();
        raytracingShader.ConstantBuffers[0] = rtConsts.InternalBuffer;
    }

    float t;
    bool sunMoving;

    public void Render()
    {
        var context = Graphics.ImmediateContext;
        var chunks = chunkManager.Chunks.Values.ToList();

        foreach (var chunk in chunks)
        {
            chunk.Mesh.Update();

            if (!chunkManager.BlockVolume.IsMapped(chunk.location))
            {
                chunkManager.BlockVolume.MapChunk(chunk.location);
                chunkManager.BlockVolume.UpdateChunk(chunk.location, chunk.blocks);
            }
        }

        context.ClearRenderTargetView(Graphics.RenderTargetView, new(0x8F, 0xD9, 0xEA));
        context.ClearDepthStencilView(depthStencilTarget.DepthStencilView, DepthStencilClearFlags.Depth, 1, 0);
        
        context.RSSetViewport(0, 0, Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);

        // World.Render(Camera);

        // do a depth-only pass to avoid overdraw when determining face visibility
        context.OMSetRenderTargets(renderTargetView: null!, depthStencilTarget.DepthStencilView);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.OMSetDepthStencilState(depthPassDSState);

        foreach (var chunk in chunks)
        {
            if (!chunk.Mesh.HasFaces)
                continue;

            depthOnlyMaterial.RenderSetup(context, Program.Camera, chunk.Transform.GetMatrix());
            chunk.Mesh.InvokeDraw(context);
        }

        // don't write to depth from here on out
        context.OMSetDepthStencilState(postDepthPassDSState);

        // do the face index pass
        foreach (var chunk in chunks)
        {
            if (!chunk.Mesh.HasFaces)
                continue;

            context.OMSetRenderTargets(renderTargetView: Graphics.RenderTargetView, depthStencilTarget.DepthStencilView);
            context.OMSetUnorderedAccessView(1, chunk.Mesh.faces?.UnorderedAccessView!);
            faceVisibilityMaterial.RenderSetup(context, Program.Camera, chunk.Transform.GetMatrix());
            chunk.Mesh.InvokeDraw(context);
        }

        context.OMSetUnorderedAccessView(1, null!);

        // dispatch raytracing compute shader 

        if (Input.IsKeyPressed(Keys.R))
            sunMoving = !sunMoving;
        if (sunMoving)
            t += Program.time - Program.lastTime;

        foreach (var chunk in chunks)
        {
            if (!chunk.Mesh.HasFaces)
                continue;

            rtConsts.Update(new()
            {
                ticks = (uint)Program.stopwatch!.ElapsedTicks,
                camPos = Program.Camera.Transform.Position,
                sunDirection = Vector3.Normalize(new(MathF.Cos(t * .75f + MathF.PI / 2), MathF.Sin(t * .75f + MathF.PI / 2), .5f)),
                age = chunk.Mesh.age,
                atlasTileSize = new(16f / chunkManager.TextureAtlas.Texture.Width, 16f / chunkManager.TextureAtlas.Texture.Height),
                chunkWidth = 2048,
                chunkHeight = 2048,
                chunkDepth = 2048,
                lightCount = (uint)chunk.Mesh.lights.Count,
                chunkX = chunk.location.X,
                chunkY = chunk.location.Y,
                chunkZ = chunk.location.Z,
                worldOriginX = chunkManager.BlockVolume.Origin.X,
                worldOriginY = chunkManager.BlockVolume.Origin.Y,
                worldOriginZ = chunkManager.BlockVolume.Origin.Z,
            });

            this.raytracingShader.SamplerStates[0] = colorMaterial.Sampler.State;

            this.raytracingShader.ResourceViews[0] = chunk.Mesh.lightBuffer?.ShaderResourceView;
            this.raytracingShader.ResourceViews[1] = chunk.Mesh.faceInfos?.ShaderResourceView;
            this.raytracingShader.ResourceViews[2] = chunkManager.TextureAtlas.Texture.ShaderResourceView;
            this.raytracingShader.ResourceViews[3] = chunkManager.BlockVolume.ShaderResourceView;

            this.raytracingShader.UnorderedAccessViews[0] = chunk.Mesh.faces!.UnorderedAccessView;

            context.SetComputeShader(this.raytracingShader);
            context.Dispatch(chunk.Mesh.faces.Width / 16, chunk.Mesh.faces.Height / 16, 1);
            context.CSSetUnorderedAccessView(0, null);
        }

        // do the color pass
        foreach (var chunk in chunks)
        {
            if (!chunk.Mesh.HasFaces)
                continue;

            context.OMSetRenderTargets(Graphics.RenderTargetView, depthStencilTarget.DepthStencilView);
            colorMaterial.PixelShader!.ResourceViews[1] = chunk.Mesh.faces!.ShaderResourceView;
            colorMaterial.RenderSetup(context, Program.Camera, chunk.Transform.GetMatrix());
            chunk.Mesh.InvokeDraw(context);
        }
    }
}


/// <summary>
/// Maintains a tiled volume texture of the world's block data, used in shaders for raycasting.
/// </summary>
class BlockMeshTiledVolume
{
    private const int TileCount = ID3D11Resource.MaximumTexture3DSize / 16;

    public readonly ID3D11ShaderResourceView ShaderResourceView;
    public readonly ID3D11Texture3D1 volume;
    private readonly TilePool tilePool;
    private readonly Dictionary<ChunkCoordinate, int> mappedTiles;

    public ChunkCoordinate Origin { get; }

    public BlockMeshTiledVolume()
    {
        tilePool = new(16);
        mappedTiles = new();

        Origin = new(64, 64, 64);

        Texture3DDescription1 desc = new()
        {
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            Depth = ID3D11Resource.MaximumTexture3DSize,
            Width = ID3D11Resource.MaximumTexture3DSize,
            Height = ID3D11Resource.MaximumTexture3DSize,
            Format = Format.R32G32B32A32_Typeless, // some channels are unused for now but this simplifies tiling since 1 chunk = 1 tile (64kb)
            MipLevels = 1,
            TextureLayout = TextureLayout.Undefined,
            MiscFlags = ResourceOptionFlags.Tiled,
        };

        volume = Graphics.Device.CreateTexture3D1(desc);

        var srvDesc = new ShaderResourceViewDescription()
        {
            Format = Format.R32G32B32A32_UInt,
            ViewDimension = ShaderResourceViewDimension.Texture3D,
            Texture3D = new()
            {
                MipLevels = 1,
                MostDetailedMip = 0,
            }
        };

        ShaderResourceView = Graphics.Device.CreateShaderResourceView(volume, srvDesc);
    }

    public void MapChunk(ChunkCoordinate location)
    {
        var resourceLocation = Origin.OffsetBy(location);
        TiledResourceCoordinate resourceCoordinate = new(resourceLocation.X, resourceLocation.Y, resourceLocation.Z, 0);
        TileRegionSize size = new(1);

        var tile = tilePool.NextTile();
        mappedTiles[location] = tile;

        var hr = Graphics.ImmediateContext.UpdateTileMappings(
            volume,
            new[] { resourceCoordinate },
            new[] { size },
            this.tilePool.Buffer,
            new[] { TileRangeFlags.ReuseSingleTile },
            new[] { tile },
            new[] { 1 },
            0
            );

        Console.WriteLine(hr);
        hr.CheckError();
    }

    public bool IsMapped(ChunkCoordinate location)
    {
        return mappedTiles.ContainsKey(location);
    }

    public void UnmapChunk(ChunkCoordinate location)
    {
        var resourceLocation = Origin.OffsetBy(location);
        TiledResourceCoordinate resourceCoordinate = new(resourceLocation.X, resourceLocation.Y, resourceLocation.Z, 0);
        TileRegionSize size = new(1);

        mappedTiles.Remove(location, out int tile);
        tilePool.ReturnTile(tile);

        var hr = Graphics.ImmediateContext.UpdateTileMappings(volume, 
            new[] { resourceCoordinate }, 
            new[] { size }, 
            this.tilePool.Buffer, 
            null!, 
            null!, 
            null!, 
            0
            );

        Console.WriteLine(hr);
        hr.CheckError();
    }

    public void UpdateChunk(ChunkCoordinate location, BlockData[] blockData)
    {
        Vortice.Mathematics.Box box = new()
        {
            Left = location.X,
            Top = location.Y,
            Front = location.Z,
            Bottom = location.Y + 16,
            Back = location.Z + 16,
            Right = location.X + 16,
        };

        Int4[] values = new Int4[16 * 16 * 16];

        for (int i = 0; i < blockData.Length; i++)
        {
            values[i] = new Int4((int)blockData[i].ID.Value,0,0,0);
        }

        //Array.Fill(values, new(0, uint.MaxValue)); // (ulong)(uint.MaxValue)<<32, 0));

        unsafe 
        {
            fixed (Int4* dataPtr = &values[0])
            {
                var resourceLocation = Origin.OffsetBy(location);
                TiledResourceCoordinate resourceCoordinate = new(resourceLocation.X, resourceLocation.Y, resourceLocation.Z, 0);
                Graphics.ImmediateContext.UpdateTiles(this.volume, resourceCoordinate, new(1), (nint)dataPtr, 0);
            }
        } 
    }

    struct Int4
    {
        public int x, y, z, w;

        public Int4(int x, int y, int z, int w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    public class TilePool : IDisposable
    {
        private const int PoolTileSize = 64 * 1024; // 64kb
        private const int ResizeThreshold = 16;

        public ID3D11Buffer Buffer { get; private set; }

        public int TileCount;
        private HashSet<int> freeTiles;

        public TilePool(int initialTileCount)
        {
            this.TileCount = initialTileCount;
            freeTiles = new(TileCount);

            for (int i = 0; i < initialTileCount; i++)
            {
                freeTiles.Add(i);
            }

            BufferDescription poolDesc = new()
            {
                BindFlags = BindFlags.None,
                ByteWidth = PoolTileSize * initialTileCount,
                CPUAccessFlags = CpuAccessFlags.None,
                StructureByteStride = 0,
                Usage = ResourceUsage.Default,
                MiscFlags = ResourceOptionFlags.TilePool,
            };

            Buffer = Graphics.Device.CreateBuffer(poolDesc);
        }

        //  Unhandled exception. SharpGen.Runtime.SharpGenException: HRESULT: [0x80070057], Module: [General], ApiCode: [E_INVALIDARG/ Invalid arguments], Message: [The parameter is incorrect.
        //]
        //   at SharpGen.Runtime.Result.ThrowFailureException()
        //   at SharpGen.Runtime.Result.CheckError()
        //   at BlockMeshTiledVolume.TilePool.NextTile() in C: \Users\Ryan -\source\repos\ConsoleApp31\ConsoleApp31\BlockMeshRenderer.cs:line 321
        //   at BlockMeshTiledVolume.MapChunk(ChunkCoordinate location) in C: \Users\Ryan -\source\repos\ConsoleApp31\ConsoleApp31\BlockMeshRenderer.cs:line 215
        //   at BlockMeshRenderer.Render() in C: \Users\Ryan -\source\repos\ConsoleApp31\ConsoleApp31\BlockMeshRenderer.cs:line 72
        //   at Program.Main() in C: \Users\Ryan -\source\repos\ConsoleApp31\ConsoleApp31\Program.cs:line 64

        // Unhandled exception.System.Runtime.InteropServices.SEHException(0x80004005): External component has thrown an exception.
        // at Vortice.Direct3D11.ID3D11DeviceContext2.UpdateTileMappings(ID3D11Resource tiledResource, Int32 numTiledResourceRegions, TiledResourceCoordinate[] tiledResourceRegionStartCoordinates, TileRegionSize[] tiledResourceRegionSizes, ID3D11Buffer tilePool, Int32 numRanges, TileRangeFlags[] rangeFlags, Int32[] tilePoolStartOffsets, Int32[] rangeTileCounts, TileMappingFlags flags)
        // at Vortice.Direct3D11.ID3D11DeviceContext2.UpdateTileMappings(ID3D11Resource tiledResource, TiledResourceCoordinate[] tiledResourceRegionStartCoordinates, TileRegionSize[] tiledResourceRegionSizes, ID3D11Buffer tilePool, TileRangeFlags[] rangeFlags, Int32[] tilePoolStartOffsets, Int32[] rangeTileCounts, TileMappingFlags flags)
        // at BlockMeshTiledVolume.MapChunk(ChunkCoordinate location) in C:\Users\Ryan-\source\repos\ConsoleApp31\ConsoleApp31\BlockMeshRenderer.cs:line 218
        // at BlockMeshRenderer.Render() in C:\Users\Ryan-\source\repos\ConsoleApp31\ConsoleApp31\BlockMeshRenderer.cs:line 72
        // at Program.Main() in C:\Users\Ryan-\source\repos\ConsoleApp31\ConsoleApp31\Program.cs:line 64

        public int NextTile()
        {
            if (!freeTiles.Any())
            {
                for (int i = TileCount; i < TileCount + ResizeThreshold; i++)
                {
                    freeTiles.Add(i);
                }

                TileCount += ResizeThreshold;

                if (TileCount != 0)
                    Graphics.ImmediateContext.ResizeTilePool(this.Buffer, (ulong)(PoolTileSize * TileCount)).CheckError();
            }

            var result = freeTiles.First();
            freeTiles.Remove(result);
            return result;
        }


        //  Unhandled exception. SharpGen.Runtime.SharpGenException: HRESULT: [0x80070057], Module: [General], ApiCode: [E_INVALIDARG/ Invalid arguments], Message: [The parameter is incorrect.
        //]
        //   at SharpGen.Runtime.Result.ThrowFailureException()
        //   at SharpGen.Runtime.Result.CheckError()
        //   at BlockMeshTiledVolume.TilePool.NextTile() in C: \Users\Ryan -\source\repos\ConsoleApp31\ConsoleApp31\BlockMeshRenderer.cs:line 321
        //   at BlockMeshTiledVolume.MapChunk(ChunkCoordinate location) in C: \Users\Ryan -\source\repos\ConsoleApp31\ConsoleApp31\BlockMeshRenderer.cs:line 215
        //   at BlockMeshRenderer.Render() in C: \Users\Ryan -\source\repos\ConsoleApp31\ConsoleApp31\BlockMeshRenderer.cs:line 72
        //   at Program.Main() in C: \Users\Ryan -\source\repos\ConsoleApp31\ConsoleApp31\Program.cs:line 64


        public void ReturnTile(int tile)
        {
            freeTiles.Add(tile);
        }

        public void Dispose()
        {
            Buffer.Dispose();
        }
    }
}