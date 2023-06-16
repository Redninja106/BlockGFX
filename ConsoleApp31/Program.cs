using ConsoleApp31.GUI;
using GLFW;
using System.Diagnostics;
using System.Numerics;
using Vortice.Direct3D11;

class Program 
{ 
    public static Window Window { get; private set; }
    public static World World { get; private set; }
    public static Camera Camera { get; private set; }

    public static float time;
    public static float lastTime;
    public static Stopwatch? stopwatch;

    public static void Main()
    {
        const int initialWidth = 1920, initialHeight = 1080;
        const bool waitForDebugger = false;

        if (waitForDebugger)
        {
            Console.ReadLine();
        }

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
        Graphics.CheckFeatureSupport();

        Glfw.SetFramebufferSizeCallback(Window, (window, width, height) => Graphics.Resize(width, height));
        
        Camera = new(.01f, 100f, MathF.PI / 2f);
        Camera.Transform.Position = new(0,8,0);

        World = new();
        var chunkManager = new BlockChunkManager();

        World.Add(chunkManager);

        Graphics.SwapInterval = 1;

        World.ClearQueues();

        ElementRenderer elemRenderer = new();
        var tex = ImageTexture.FromFile("Assets/crosshair.png");

        BlockMeshRenderer renderer = new(chunkManager);
        //TiledTextureTest test = new(elemRenderer);

        while (!Glfw.WindowShouldClose(Window))
        {
            Input.Update();
            Glfw.PollEvents();

            var context = Graphics.ImmediateContext;
            context.ClearState();
            
            Update();
            renderer.Render();
            //test.Render();

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

        Camera.Update(deltaTime);
        World.Update(deltaTime);
    }

}

class TiledTextureTest
{
    private ID3D11Texture2D tiledTexture;
    private ID3D11Buffer tilePool;
    private ID3D11ShaderResourceView view;
    public ElementRenderer ElemRenderer { get; }
    const int tileSize = 64 * 1024;

    public TiledTextureTest(ElementRenderer elemRenderer)
    {
        x = y = 0;


        var texDesc = new Texture2DDescription()
        {
            ArraySize = 1,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            Format = Vortice.DXGI.Format.R8G8B8A8_UNorm,
            Height = 1000,
            Width = 1000,
            MipLevels = 1,
            MiscFlags = ResourceOptionFlags.Tiled,
            SampleDescription = new(1, 0),
            Usage = ResourceUsage.Default
        };

        tiledTexture = Graphics.Device.CreateTexture2D(in texDesc);

        view = Graphics.Device.CreateShaderResourceView(tiledTexture, new()
        {
            Format = texDesc.Format,
            ViewDimension = Vortice.Direct3D.ShaderResourceViewDimension.Texture2D,
            Texture2D = new()
            {
                MipLevels = 1,
                MostDetailedMip = 0
            }
        });

        var bufferDesc = new BufferDescription()
        {
            BindFlags = BindFlags.None,
            ByteWidth = tileSize * 1,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.TilePool,
            StructureByteStride = 0,
            Usage = ResourceUsage.Default
        };

        tilePool = Graphics.Device.CreateBuffer(bufferDesc);
        ElemRenderer = elemRenderer;

        var context = Graphics.ImmediateContext;

        context.UpdateTileMappings(
            tiledTexture,
            new[] { new TiledResourceCoordinate(x, y, 0, 0) },
            new[] { new TileRegionSize(1, 1, 1) },
            tilePool,
            new[] { TileRangeFlags.ReuseSingleTile },
            new[] { 0 },
            new[] { 1 },
            TileMappingFlags.None
            );

        uint[] data = new uint[tileSize / 4];
        Array.Fill(data, uint.MaxValue);
        unsafe 
        {
            fixed (uint* dataPtr = &data[0])
            {
                context.UpdateTiles(tiledTexture, new(x, y, 0, 0), new(1), (nint)dataPtr, TileCopyFlags.None);
            }
        }
    }

    int x, y;

    public void Render()
    {
        ElemRenderer.DrawTexture(new(0, 0, 1, 1), new(.05f, .15f, .95f, .65f), view);

        var context = Graphics.ImmediateContext;
        if (Input.IsKeyPressed(Keys.W))
        {
            y++;
            context.CopyTileMappings(
                tiledTexture,
                new(x, y, 0, 0),
                tiledTexture,
                new(x, y-1, 0, 0),
                new(1)
                ).CheckError();
        }
    }
}

struct RaytracingConstants
{
    public Vector3 camPos;
    public uint ticks;
    public Vector3 sunDirection;
    public uint age;
    public Vector2 atlasTileSize;
    public uint lightCount;
    private int _pad3;
    public uint chunkWidth;
    public uint chunkHeight;
    public uint chunkDepth;
    private int _pad4;
    public int chunkX;
    public int chunkY;
    public int chunkZ;
    private int _pad5;
    public int worldOriginX;
    public int worldOriginY;
    public int worldOriginZ;
}

struct FaceInfo
{
    public Vector3 position;
    public Vector3 up;
    public Vector3 right;
    public uint atlasLocationX;
    public uint atlasLocationY;
}