using ConsoleApp31;
using ConsoleApp31.Drawing;
using ConsoleApp31.Drawing.Materials;
using ConsoleApp31.Extensions;
using ConsoleApp31.GUI;
using ConsoleApp31.Texturing;
using GLFW;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

class Program 
{ 
    public static Window Window { get; private set; }
    public static World World { get; private set; }
    public static Camera Camera { get; private set; }

    public static float time;
    public static float lastTime;
    public static Stopwatch? stopwatch;
    private static DepthStencilTexture depthStencilTexture;

    public static void Main()
    {
        const int initialWidth = 1920, initialHeight = 1080;

        if (!Glfw.Init())
        {
            throw new System.Exception("Failed to initialize GLFW!");
        }

        Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
        Window = Glfw.CreateWindow(initialWidth, initialHeight, "ConsoleApp31", GLFW.Monitor.None, Window.None);
        
        Input.Initialize(Window);

        nint hwnd = Native.GetWin32Window(Window);
        Graphics.Initialize();
        Graphics.CreateSwapChain(hwnd, initialWidth, initialHeight);
        
        Glfw.SetFramebufferSizeCallback(Window, (window, width, height) => Graphics.Resize(width, height));

        Camera = new(.01f, 100f, MathF.PI / 2f);
        Camera.Transform.Position = new(0,8,0);

        World = new();
        var chunkManager = new BlockChunkManager();

        World.Add(chunkManager);

        Graphics.SwapInterval = 0;

        World.ClearQueues();

        ElementRenderer elemRenderer = new();
        var tex = ImageTexture.FromFile("Assets/crosshair.png");

        BlockChunkRenderer renderer = new(chunkManager);

        while (!Glfw.WindowShouldClose(Window))
        {
            Input.Update();
            Glfw.PollEvents();

            var context = Graphics.ImmediateContext;
            context.ClearState();
            
            Update();
            renderer.Render();

            float size = .025f;
            elemRenderer.DrawTexture(new(0, 0, 1, 1), new(.5f - size / 2f, .5f - size / 2f, size, size), tex);

            Graphics.Present();
        }

        World.Dispose();
        Graphics.Destroy();
        Glfw.Terminate();
    }

    private static void Update()
    {
        stopwatch ??= Stopwatch.StartNew();

        float deltaTime = time - lastTime;
        lastTime = time;
        time = (stopwatch.ElapsedTicks / (float)Stopwatch.Frequency);

        Glfw.SetWindowTitle(Window, $"blockgfx - {1f / deltaTime}FPS");

        Camera.Update(deltaTime);
        World.Update(deltaTime);
    }

}

class BlockChunkRenderer
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

    public BlockChunkRenderer(BlockChunkManager chunkManager)
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

    public void Render()
    {
        var context = Graphics.ImmediateContext;
        var chunk = chunkManager.Chunks.Values.Single();
        chunk.Mesh.Update();

        if (Input.IsKeyPressed(Keys.R))
        {
            raytracingShader.Reload();
        }

        context.ClearRenderTargetView(Graphics.RenderTargetView, new(0x8F, 0xD9, 0xEA));
        context.ClearDepthStencilView(depthStencilTarget.DepthStencilView, DepthStencilClearFlags.Depth, 1, 0);
        
        context.RSSetViewport(0, 0, Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);

        //World.Render(Camera);

        // do a depth-only pass to avoid overdraw when determining face visibility
        context.OMSetRenderTargets(renderTargetView: null!, depthStencilTarget.DepthStencilView);

        context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.OMSetDepthStencilState(depthPassDSState);

        depthOnlyMaterial.RenderSetup(context, Program.Camera, chunk.Transform.GetMatrix());
        chunk.Mesh.InvokeDraw(context);

        // don't write to depth from here on out
        context.OMSetDepthStencilState(postDepthPassDSState);

        // do the face index pass
        context.OMSetRenderTargets(renderTargetView: Graphics.RenderTargetView, depthStencilTarget.DepthStencilView);
        context.OMSetUnorderedAccessView(1, chunk.Mesh.faces!.UnorderedAccessView);
        faceVisibilityMaterial.RenderSetup(context, Program.Camera, chunk.Transform.GetMatrix());
        chunk.Mesh.InvokeDraw(context);
        context.OMSetUnorderedAccessView(1, null!);

        // dispatch raytracing compute shader 
        rtConsts.Update(new()
        {
            ticks = (uint)Program.stopwatch!.ElapsedTicks,
            camPos = Program.Camera.Transform.Position,
            sunDirection = Vector3.Normalize(new(MathF.Cos(Program.time * .75f + MathF.PI / 2), MathF.Sin(Program.time * .75f + MathF.PI / 2), .5f)),
            age = chunk.Mesh.age,
            atlasTileSize = new(16f / chunkManager.TextureAtlas.Texture.Width, 16f / chunkManager.TextureAtlas.Texture.Height),
            chunkWidth = BlockChunk.Width,
            chunkHeight = BlockChunk.Height,
            chunkDepth = BlockChunk.Depth,
        });

        Console.WriteLine(chunk.Mesh.age);

        this.raytracingShader.SamplerStates[0] = colorMaterial.Sampler.State;
        this.raytracingShader.ResourceViews[0] = chunk.Mesh.hitBoxBuffer.ShaderResourceView;
        this.raytracingShader.ResourceViews[1] = chunk.Mesh.faceInfos.ShaderResourceView;
        this.raytracingShader.ResourceViews[2] = chunkManager.TextureAtlas.Texture.ShaderResourceView;
        this.raytracingShader.ResourceViews[3] = chunk.blockVolume.ShaderResourceView;

        this.raytracingShader.UnorderedAccessViews[0] = chunk.Mesh.faces!.UnorderedAccessView;

        context.SetComputeShader(this.raytracingShader);
        context.Dispatch(chunk.Mesh.faces.Width / 16, 1, 1);
        context.CSSetUnorderedAccessView(0, null);

        // do the color pass
        context.OMSetRenderTargets(Graphics.RenderTargetView, depthStencilTarget.DepthStencilView);
        colorMaterial.PixelShader!.ResourceViews[1] = chunk.Mesh.faces!.ShaderResourceView;
        colorMaterial.RenderSetup(context, Program.Camera, chunk.Transform.GetMatrix());
        chunk.Mesh.InvokeDraw(context);
    }

    Vector3 p1, p2;

    struct Int3
    {
        public int X, Y, Z;

        public Int3(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3 ToVector()
        {
            return new(X, Y, Z);
        }
    }

    bool raycast(BlockChunk chunk, Ray ray, out float outT, out Vector3 outNormal, out Int3 outVoxel)
    {
        outT = default;
        outNormal = default;
        outVoxel = default;

        Int3 voxel = new Int3((int)MathF.Floor(ray.origin.X), (int)MathF.Floor(ray.origin.Y), (int)MathF.Floor(ray.origin.Z));
        Int3 step = new(MathF.Sign(ray.direction.X), MathF.Sign(ray.direction.Y), MathF.Sign(ray.direction.Z));

        if (step.X == 0 && step.Y == 0 && step.Z == 0)
        {
            return false;
        }

        float tNear, tFar;
        Box box;
        box.min = new Vector3(0);
        box.max = new Vector3(16);
        CalcNearFar(ray, box, out tNear, out tFar);

        Vector3 start = ray.At(MathF.Max(0, tNear));
        Vector3 end = ray.At(tFar);

        Vector3 d = end - start;
        Vector3 tDelta = step.ToVector() / d;
        Vector3 tMax = tDelta * getMax(start, step.ToVector());

        int dist = 100;

        float t = 0;

        while (--dist > 0)
        {
            if (voxel.X < 0 || voxel.X >= 16 || voxel.Y < 0 || voxel.Y >= 16 || voxel.Z < 0 || voxel.Z >= 16)
            {
                return false;
            }

            if (!chunk[voxel.Z, voxel.X, voxel.Y].IsTransparent)
            {
                outT = t;
                outVoxel = voxel;
                return true;
            }

            if (tMax.X < tMax.Y)
            {
                if (tMax.X < tMax.Z)
                {
                    voxel.X += step.X;
                    tMax.X += tDelta.X;
                    t += tDelta.X;
                    outNormal = new Vector3(-step.X, 0, 0);
                }
                else
                {
                    voxel.Z += step.Z;
                    tMax.Z += tDelta.Z;
                    t += tDelta.Z;
                    outNormal = new Vector3(0, 0, -step.Z);
                }
            }
            else
            {
                if (tMax.Y < tMax.Z)
                {
                    voxel.Y += step.Y;
                    tMax.Y += tDelta.Y;
                    t += tDelta.Y;
                    outNormal = new Vector3(0, -step.Y, 0);
                }
                else
                {
                    voxel.Z += step.Z;
                    tMax.Z += tDelta.Z;
                    t += tDelta.Z;
                    outNormal = new Vector3(0, 0, -step.Z);
                }
            }
        }

        return false;
    }
    Vector3 getMax(Vector3 start, Vector3 step)
    {
        return new Vector3(
            step.X > 0 ? 1 - (start.X % 1f) : start.X % 1f,
            step.Y > 0 ? 1 - (start.Y % 1f) : start.Y % 1f,
            step.Z > 0 ? 1 - (start.Z % 1f) : start.Z % 1f
            );
    }
    private void CalcNearFar(Ray ray, Box box, out float near, out float far)
    {
        float t1 = (box.min.X - ray.origin.X) * ray.inverseDirection.X;
        float t2 = (box.max.X - ray.origin.X) * ray.inverseDirection.X;
        float t3 = (box.min.Y - ray.origin.Y) * ray.inverseDirection.Y;
        float t4 = (box.max.Y - ray.origin.Y) * ray.inverseDirection.Y;
        float t5 = (box.min.Z - ray.origin.Z) * ray.inverseDirection.Z;
        float t6 = (box.max.Z - ray.origin.Z) * ray.inverseDirection.Z;

        near = MathF.Max(MathF.Max(MathF.Min(t1, t2), MathF.Min(t3, t4)), MathF.Min(t5, t6));
        far = MathF.Min(MathF.Min(MathF.Max(t1, t2), MathF.Max(t3, t4)), MathF.Max(t5, t6));

    }
}

struct RaytracingConstants
{
    public Vector3 camPos;
    public uint ticks;
    public Vector3 sunDirection;
    public uint age;
    public Vector2 atlasTileSize;
    private int _pad2, _pad3;
    public uint chunkWidth;
    public uint chunkHeight;
    public uint chunkDepth;
}

struct FaceInfo
{
    public Vector3 position;
    public Vector3 up;
    public Vector3 right;
    public uint atlasLocationX;
    public uint atlasLocationY;
}