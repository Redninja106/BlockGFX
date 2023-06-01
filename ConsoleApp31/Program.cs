using ConsoleApp31;
using ConsoleApp31.Drawing;
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
        
        Glfw.SetFramebufferSizeCallback(Window, (window, width, height) => Graphics.Resize(width, height));

        depthStencilTexture = new DepthStencilTexture(initialWidth, initialHeight);
        Graphics.AfterResize += () =>
        {
            depthStencilTexture?.Dispose();
            depthStencilTexture = new(Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);
        };

        Camera = new(.01f, 100f, MathF.PI / 2f);
        Camera.Transform.Position = new(0,8,0);

        World = new();
        var chunkManager = new BlockChunkManager();

        World.Add(chunkManager);

        Graphics.SwapInterval = 1;

        World.ClearQueues();

        ElementRenderer renderer = new();
        var tex = ImageTexture.FromFile("Assets/crosshair.png");
        
        while (!Glfw.WindowShouldClose(Window))
        {
            Input.Update();
            Glfw.PollEvents();

            var context = Graphics.ImmediateContext;

            Update();
            Render();

            float size = .025f;
            renderer.DrawTexture(new(0, 0, 1, 1), new(.5f - size / 2f, .5f - size / 2f, size, size), tex);

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

        Camera.Update(deltaTime);
        World.Update(deltaTime);
    }

    private static void Render()
    {
        var context = Graphics.ImmediateContext;

        context.ClearRenderTargetView(Graphics.RenderTargetView, new(0x8F, 0xD9, 0xEA));
        context.ClearDepthStencilView(depthStencilTexture.DepthStencilView, DepthStencilClearFlags.Depth, 1, 0);

        context.OMSetRenderTargets(Graphics.RenderTargetView, depthStencilTexture.DepthStencilView);

        context.RSSetViewport(0, 0, Graphics.RenderTargetWidth, Graphics.RenderTargetHeight);

        World.Render(Camera);
    }
}
struct Vertex
{
    public Vector3 position;
    public Vector2 uv;

    public Vertex(Vector3 position, Vector2 uv)
    {
        this.position = position;
        this.uv = uv;
    }
}