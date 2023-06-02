using ConsoleApp31;
using ConsoleApp31.Drawing;
using ConsoleApp31.Drawing.Deferred;
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
using Vortice;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

class Program 
{ 
    public static Window Window { get; private set; }
    public static World World { get; private set; }
    public static Camera Camera { get; private set; }

    private static float time;
    private static float lastTime;
    private static Stopwatch? stopwatch;
    private static DepthStencilTexture depthStencilTexture;

    public static void Main()
    {
        const int initialWidth = 1920, initialHeight = 1080;

        if (!Glfw.Init())
        {
            throw new System.Exception("Failed to initialize GLFW!");
        }

        Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
        Window = Glfw.CreateWindow(initialWidth, initialHeight, "ConsoleApp31", Monitor.None, Window.None);
        
        Input.Initialize(Window);

        nint hwnd = Native.GetWin32Window(Window);
        Graphics.Initialize();
        Graphics.CreateSwapChain(hwnd, initialWidth, initialHeight);

        Sampler.InitalizeSharedSamplers();
        PrimitiveMeshes.Initialize();

        Glfw.SetFramebufferSizeCallback(Window, (window, width, height) => Graphics.Resize(width, height));

        depthStencilTexture = new DepthStencilTexture(initialWidth, initialHeight);
        

        Camera = new(.01f, 100f, MathF.PI / 2f);
        Camera.Transform.Position = new(0,8,0);

        World = new();
        var chunkManager = new BlockChunkManager();

        World.Add(chunkManager);

        Graphics.SwapInterval = 1;

        World.ClearQueues();

        ElementRenderer renderer = new();
        var tex = ImageTexture.FromFile("Assets/crosshair.png");
        var stone = ImageTexture.FromFile("Assets/stone.png");

        var gbuffer = new GBuffer(initialWidth, initialHeight);

        Graphics.AfterResize += () =>
        {
            depthStencilTexture?.Dispose();
            depthStencilTexture = new(Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);
            gbuffer.Resize(Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);
        };
        var constantBuffer = new ConstantBuffer<GBufferConstantData>();

        var lightVolumeMaterial = new DeferredMaterial(gbuffer, constantBuffer, new("chunk_vs.hlsl"), new("deferred_diffuse_ps.hlsl"));
        var ambientQuadMaterial = new DeferredMaterial(gbuffer, constantBuffer, new("fullscreen_quad_vs.hlsl"), new("deferred_ambient_ps.hlsl"));

        var backsideRS = Graphics.Device.CreateRasterizerState(RasterizerDescription.CullFront);

        BlendDescription blend = new();
        ref var rt = ref blend.RenderTarget[0];
        rt.SourceBlend = Blend.One;
        rt.DestinationBlend = Blend.One;
        rt.BlendOperation = BlendOperation.Add;
        rt.SourceBlendAlpha = Blend.One;
        rt.DestinationBlendAlpha = Blend.One;
        rt.BlendOperationAlpha = BlendOperation.Add;
        rt.RenderTargetWriteMask = ColorWriteEnable.All;

        var diffuseBlendState = Graphics.Device.CreateBlendState(new(Blend.One, Blend.One));
        var depthStencilStateDisabled = Graphics.Device.CreateDepthStencilState(new(false, DepthWriteMask.Zero));
        var depthStencilStateLight = Graphics.Device.CreateDepthStencilState(new(true, DepthWriteMask.Zero, depthFunc: ComparisonFunction.GreaterEqual));

        while (!Glfw.WindowShouldClose(Window))
        {
            Input.Update();
            Glfw.PollEvents();

            var context = Graphics.ImmediateContext;
            context.ClearState();

            Update();

            gbuffer.Clear(context);
            context.ClearDepthStencilView(depthStencilTexture.DepthStencilView, DepthStencilClearFlags.Depth, 1, 0);

            gbuffer.ApplyRenderTargets(context, depthStencilTexture.DepthStencilView);
            context.RSSetViewport(0, 0, Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);

            World.Render(Camera);

            context.OMSetRenderTargets(Graphics.RenderTargetView, depthStencilTexture.DepthStencilView);
            context.RSSetViewport(0, 0, Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);

            constantBuffer.Update(new() 
            { 
                InverseDisplaySize = new(1f / Graphics.RenderTargetWidth, 1f / Graphics.RenderTargetHeight) 
            });

            context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            //context.SetVertexShader(quadVS);
            
            context.OMSetDepthStencilState(depthStencilStateDisabled);
            ambientQuadMaterial.RenderSetup(context, Camera, default);
            context.Draw(3, 0);

            context.OMSetBlendState(diffuseBlendState);
            context.RSSetState(backsideRS);
            context.OMSetDepthStencilState(null);
            context.ClearDepthStencilView(depthStencilTexture.DepthStencilView, DepthStencilClearFlags.Depth, 1, 0);

            // render one light
            lightVolumeMaterial.RenderSetup(context, Camera, Matrix4x4.CreateScale(5f) * Matrix4x4.CreateTranslation(0, 5, 0));
            PrimitiveMeshes.Sphere.InvokeDraw(context);

            context.ClearDepthStencilView(depthStencilTexture.DepthStencilView, DepthStencilClearFlags.Depth, 1, 0);

            float size = .025f;
            renderer.DrawTexture(new(0, 0, 1, 1), new(.5f - size / 2f, .5f - size / 2f, size, size), tex);

            Graphics.Present();
        }

        World.Dispose();
        Graphics.Destroy();
        Glfw.Terminate();

        void DepthPass()
        {

        }

        void RenderLightVolumes()
        {

        }
    }

    PointLight[] lights = new PointLight[]
    {
        new(new(0, 5, 0), 5)
    };


    private static void Update()
    {
        stopwatch ??= Stopwatch.StartNew();

        float deltaTime = time - lastTime;
        lastTime = time;
        time = (stopwatch.ElapsedTicks / (float)Stopwatch.Frequency);

        Camera.Update(deltaTime);
        World.Update(deltaTime);
    }
}